using Ka3005P.Core.Device;
using Ka3005P.Core.Protocol;

namespace Ka3005P.App.Demo;

public sealed class DemoPowerSupplyDevice : IPowerSupplyDevice
{
	private readonly TimeSpan operationDelay;
	private readonly object gate=new();
	private int voltageHundredths;
	private int currentThousandths;
	private bool outputEnabled;

	public DemoPowerSupplyDevice(TimeSpan operationDelay)
	{
		if(operationDelay<TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(operationDelay));
		}
		this.operationDelay=operationDelay;
	}

	public async ValueTask SetVoltageAsync(
		VoltageSetpoint value,
		CancellationToken cancellationToken)
	{
		await DelayAsync(cancellationToken);
		lock(gate)
		{
			voltageHundredths=value.Hundredths;
		}
	}

	public async ValueTask SetCurrentAsync(
		CurrentSetpoint value,
		CancellationToken cancellationToken)
	{
		await DelayAsync(cancellationToken);
		lock(gate)
		{
			currentThousandths=value.Thousandths;
		}
	}

	public async ValueTask SetOutputAsync(
		bool enabled,
		CancellationToken cancellationToken)
	{
		await DelayAsync(cancellationToken);
		lock(gate)
		{
			outputEnabled=enabled;
		}
	}

	public async ValueTask<DeviceMeasurement> ReadMeasurementAsync(
		CancellationToken cancellationToken)
	{
		await DelayAsync(cancellationToken);
		lock(gate)
		{
			if(!outputEnabled)
			{
				return new DeviceMeasurement(0,0);
			}
			int loadCurrent=Math.Max(0,voltageHundredths/5);
			return new DeviceMeasurement(
				voltageHundredths,
				Math.Min(currentThousandths,loadCurrent));
		}
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	private ValueTask DelayAsync(CancellationToken cancellationToken)
	{
		return operationDelay == TimeSpan.Zero
			? ValueTask.CompletedTask
			: new ValueTask(Task.Delay(operationDelay,cancellationToken));
	}
}
