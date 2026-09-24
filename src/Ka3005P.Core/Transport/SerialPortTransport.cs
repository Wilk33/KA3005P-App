using System.IO.Ports;

namespace Ka3005P.Core.Transport;

public sealed class SerialPortTransport : ISerialTransport
{
	private readonly TimeSpan readTimeout;
	private readonly TimeSpan writeTimeout;
	private SerialPort? port;

	public SerialPortTransport()
		: this(TimeSpan.FromMilliseconds(250),TimeSpan.FromMilliseconds(250))
	{
	}

	public SerialPortTransport(TimeSpan readTimeout,TimeSpan writeTimeout)
	{
		if(readTimeout <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(readTimeout));
		}
		if(writeTimeout <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(writeTimeout));
		}

		this.readTimeout=readTimeout;
		this.writeTimeout=writeTimeout;
	}

	public async ValueTask OpenAsync(string portName,CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(portName);
		if(port is not null)
		{
			throw new InvalidOperationException("Transport jest już otwarty.");
		}

		SerialPort created=new(portName,9600,Parity.None,8,StopBits.One)
		{
			Handshake=Handshake.None,
			DtrEnable=false,
			RtsEnable=false,
			ReadTimeout=ToMilliseconds(readTimeout),
			WriteTimeout=ToMilliseconds(writeTimeout)
		};

		try
		{
			await Task.Run(created.Open,cancellationToken).ConfigureAwait(false);
			port=created;
		}
		catch
		{
			created.Dispose();
			throw;
		}
	}

	public async ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer,
		CancellationToken cancellationToken)
	{
		SerialPort opened=GetOpenPort();
		await opened.BaseStream.WriteAsync(buffer,cancellationToken).ConfigureAwait(false);
		await opened.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
	}

	public ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken)
	{
		return GetOpenPort().BaseStream.ReadAsync(buffer,cancellationToken);
	}

	public ValueTask CloseAsync()
	{
		SerialPort? opened=port;
		port=null;
		if(opened is not null)
		{
			opened.Close();
			opened.Dispose();
		}

		return ValueTask.CompletedTask;
	}

	public async ValueTask DisposeAsync()
	{
		await CloseAsync().ConfigureAwait(false);
	}

	private SerialPort GetOpenPort()
	{
		if(port is null || !port.IsOpen)
		{
			throw new InvalidOperationException("Port szeregowy nie jest otwarty.");
		}

		return port;
	}

	private static int ToMilliseconds(TimeSpan timeout)
	{
		double milliseconds=Math.Ceiling(timeout.TotalMilliseconds);
		if(milliseconds > int.MaxValue)
		{
			return int.MaxValue;
		}

		return Math.Max(1,(int)milliseconds);
	}
}
