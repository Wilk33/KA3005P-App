using Ka3005P.App.Demo;
using Ka3005P.App.Services;

namespace Ka3005P.Tests.Services;

public sealed class SerialPortCatalogTests
{
	[Fact]
	public void Normalize_DeduplicatesAndNaturallySortsPortNames()
	{
		IReadOnlyList<string> result=SystemSerialPortCatalog.Normalize(
			["com10","COM2","COM2"," COM1 "]);

		Assert.Equal(["COM1","COM2","COM10"],result);
	}

	[Fact]
	public void DemoCatalog_ReturnsStablePorts()
	{
		Assert.Equal(
			["COM1","COM2","COM3","COM4","COM5","COM6"],
			new DemoSerialPortCatalog().GetPortNames());
	}
}
