using Ka3005P.Core.Device;
using Ka3005P.Core.Protocol;

namespace Ka3005P.Core.Sessions;

public sealed class PowerSupplySession : IPowerSupplySession
{
	private readonly IPowerSupplyDevice device;
	private readonly TimeProvider timeProvider;
	private readonly SessionRequestQueue requests=new();
	private readonly object snapshotGate=new();
	private readonly object lifecycleGate=new();
	private SessionSnapshot snapshot=new();
	private Task? runTask;
	private bool disposed;

	public PowerSupplySession(IPowerSupplyDevice device,TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(timeProvider);
		this.device=device;
		this.timeProvider=timeProvider;
	}

	public event EventHandler<SessionSnapshot>? SnapshotChanged;

	public SessionSnapshot Snapshot
	{
		get
		{
			lock(snapshotGate)
			{
				return snapshot;
			}
		}
	}

	public int PendingSetpointCount => requests.PendingSetpointCount;

	public ValueTask StartAsync(CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(disposed,this);
		cancellationToken.ThrowIfCancellationRequested();
		lock(lifecycleGate)
		{
			if(runTask is not null)
			{
				throw new InvalidOperationException("Sesja została już uruchomiona.");
			}

			runTask=RunAsync();
		}
		Publish(current=>current with { IsRunning=true });
		return ValueTask.CompletedTask;
	}

	public async ValueTask StopAsync(CancellationToken cancellationToken)
	{
		Task? running;
		lock(lifecycleGate)
		{
			running=runTask;
		}

		if(running is null)
		{
			return;
		}

		requests.Close();
		await running.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public void RequestVoltage(VoltageSetpoint value)
	{
		EnsureRunning();
		Publish(current=>current with
		{
			RequestedVoltage=value,
			Error=null
		});
		requests.SetVoltage(value);
	}

	public void RequestCurrent(CurrentSetpoint value)
	{
		EnsureRunning();
		Publish(current=>current with
		{
			RequestedCurrent=value,
			Error=null
		});
		requests.SetCurrent(value);
	}

	public void RequestMeasurement()
	{
		EnsureRunning();
		requests.RequestMeasurement();
	}

	public async ValueTask SetOutputAsync(
		bool enabled,
		CancellationToken cancellationToken)
	{
		EnsureRunning();
		Publish(current=>current with
		{
			RequestedOutput=enabled,
			Error=null
		});
		SetOutputRequest request=requests.SetOutput(enabled);
		await request.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask DisposeAsync()
	{
		if(disposed)
		{
			return;
		}

		disposed=true;
		await StopAsync(CancellationToken.None).ConfigureAwait(false);
		await device.DisposeAsync().ConfigureAwait(false);
	}

	private async Task RunAsync()
	{
		try
		{
			while(true)
			{
				await requests.WaitAsync(CancellationToken.None).ConfigureAwait(false);
				while(requests.TryTakeNext(out SessionRequest? request))
				{
					if(request is CloseSessionRequest)
					{
						return;
					}

					await ExecuteAsync(request).ConfigureAwait(false);
				}
			}
		}
		finally
		{
			Publish(current=>current with { IsRunning=false });
		}
	}

	private async ValueTask ExecuteAsync(SessionRequest request)
	{
		try
		{
			switch(request)
			{
				case SetVoltageRequest voltage:
					await device.SetVoltageAsync(
						voltage.Value,
						CancellationToken.None).ConfigureAwait(false);
					Publish(current=>current with
					{
						SentVoltage=voltage.Value,
						Error=null
					});
					break;
				case SetCurrentRequest current:
					await device.SetCurrentAsync(
						current.Value,
						CancellationToken.None).ConfigureAwait(false);
					Publish(value=>value with
					{
						SentCurrent=current.Value,
						Error=null
					});
					break;
				case SetOutputRequest output:
					await device.SetOutputAsync(
						output.Enabled,
						CancellationToken.None).ConfigureAwait(false);
					Publish(current=>current with
					{
						OutputState=output.Enabled ? OutputState.On : OutputState.Off,
						Error=null
					});
					output.Completion.TrySetResult();
					break;
				case MeasurementRequest:
					DeviceMeasurement measurement=
						await device.ReadMeasurementAsync(CancellationToken.None).ConfigureAwait(false);
					Publish(current=>current with
					{
						LastMeasurement=measurement,
						LastMeasurementAt=timeProvider.GetUtcNow(),
						Error=null
					});
					break;
				default:
					throw new InvalidOperationException(
						$"Nieznane żądanie sesji: {request.GetType().Name}.");
			}
		}
		catch(Exception exception)
		{
			Publish(current=>current with
			{
				Error=new SessionError(
					timeProvider.GetUtcNow(),
					exception.Message,
					exception)
			});
			if(request is SetOutputRequest output)
			{
				output.Completion.TrySetException(exception);
			}
		}
	}

	private void EnsureRunning()
	{
		ObjectDisposedException.ThrowIf(disposed,this);
		lock(lifecycleGate)
		{
			if(runTask is null || runTask.IsCompleted)
			{
				throw new InvalidOperationException("Sesja nie jest uruchomiona.");
			}
		}
	}

	private void Publish(Func<SessionSnapshot,SessionSnapshot> update)
	{
		SessionSnapshot published;
		lock(snapshotGate)
		{
			snapshot=update(snapshot);
			published=snapshot;
		}

		SnapshotChanged?.Invoke(this,published);
	}
}
