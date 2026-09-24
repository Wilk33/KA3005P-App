using Ka3005P.App.Demo;
using Ka3005P.Core.Device;
using Ka3005P.Core.Protocol;

namespace Ka3005P.Tests.Demo;

public sealed class DemoPowerSupplyDeviceTests
{
	[Fact]
	public async Task Measurement_IsZeroWhileOffAndUsesSetpointsWhileOn()
	{
		DemoPowerSupplyDevice device=new(TimeSpan.Zero);
		await device.SetVoltageAsync(
			VoltageSetpoint.FromHundredths(1200),
			CancellationToken.None);
		await device.SetCurrentAsync(
			CurrentSetpoint.FromThousandths(1000),
			CancellationToken.None);

		DeviceMeasurement off=await device.ReadMeasurementAsync(
			CancellationToken.None);
		await device.SetOutputAsync(true,CancellationToken.None);
		DeviceMeasurement on=await device.ReadMeasurementAsync(
			CancellationToken.None);

		Assert.Equal(new DeviceMeasurement(0,0),off);
		Assert.Equal(1200,on.VoltageHundredths);
		Assert.InRange(on.CurrentThousandths,100,1000);
	}
}
