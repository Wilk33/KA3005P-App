using System.Globalization;
using Ka3005P.Core.Dual;
using Ka3005P.Core.Measurements;

namespace Ka3005P.Tests.Measurements;

public sealed class CsvMeasurementWriterTests
{
	[Fact]
	public async Task WriteSingleAsync_UsesSemicolonAndInvariantNumbers()
	{
		CultureInfo original=CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("pl-PL");
		try
		{
			StringWriter output=new();
			CsvMeasurementWriter writer=new(output);
			await writer.WriteHeaderAsync(
				MeasurementLayout.Single,
				MeasurementExportKind.Voltage,
				CancellationToken.None);
			await writer.WriteAsync(
				new MeasurementSample(TimeSpan.FromMilliseconds(150),1234,123),
				MeasurementExportKind.Voltage,
				CancellationToken.None);

			Assert.Equal(
				"Time;Voltage;\n[s];[V];\n0.150;12.34;\n",
				output.ToString());
		}
		finally
		{
			CultureInfo.CurrentCulture=original;
		}
	}

	[Fact]
	public async Task WriteDualAsync_UsesModeSpecificColumnsAndSignedSymmetricBranches()
	{
		StringWriter output=new();
		CsvMeasurementWriter writer=new(output);
		await writer.WriteHeaderAsync(
			MeasurementLayout.Symmetric,
			MeasurementExportKind.Voltage,
			CancellationToken.None);
		MeasurementSample first=new(TimeSpan.FromSeconds(1),1200,400);
		MeasurementSample second=new(TimeSpan.FromSeconds(1),1100,350);
		DualMeasurement measurement=DualMeasurement.Aggregate(
			DualMode.Symmetric,
			first,
			second);

		await writer.WriteAsync(
			measurement,
			MeasurementExportKind.Voltage,
			CancellationToken.None);

		Assert.Equal(
			"Time;Voltage -;Voltage +;\n[s];[V];[V];\n1.000;-12.00;11.00;\n",
			output.ToString());
	}

	[Fact]
	public async Task WriteMissingAsync_LeavesMeasurementColumnsEmpty()
	{
		StringWriter output=new();
		CsvMeasurementWriter writer=new(output);

		await writer.WriteMissingAsync(
			TimeSpan.FromSeconds(2),
			MeasurementLayout.Series,
			MeasurementExportKind.Voltage,
			CancellationToken.None);

		Assert.Equal("2.000;;;;\n",output.ToString());
	}
}
