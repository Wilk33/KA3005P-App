using Ka3005P.App.Services;

namespace Ka3005P.App.Demo;

public sealed class DemoSerialPortCatalog : ISerialPortCatalog
{
	private static readonly string[] Ports=["COM5","COM6","COM7"];

	public IReadOnlyList<string> GetPortNames()
	{
		return Ports;
	}
}
