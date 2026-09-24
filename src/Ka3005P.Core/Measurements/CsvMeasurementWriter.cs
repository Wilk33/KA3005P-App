using System.Globalization;
using Ka3005P.Core.Dual;

namespace Ka3005P.Core.Measurements;

public enum MeasurementLayout
{
	Single,
	Series,
	Parallel,
	Symmetric
}

public enum MeasurementExportKind
{
	Voltage,
	Current
}

public sealed class CsvMeasurementWriter
{
	private readonly TextWriter output;

	public CsvMeasurementWriter(TextWriter output)
	{
		ArgumentNullException.ThrowIfNull(output);
		this.output=output;
	}

	public ValueTask WriteHeaderAsync(
		MeasurementLayout layout,
		MeasurementExportKind kind,
		CancellationToken cancellationToken)
	{
		(string labels,string units)=GetHeader(layout,kind);
		return WriteTextAsync(labels+"\n"+units+"\n",cancellationToken);
	}

	public ValueTask WriteAsync(
		MeasurementSample sample,
		MeasurementExportKind kind,
		CancellationToken cancellationToken)
	{
		string value=kind == MeasurementExportKind.Voltage
			? FormatVoltage(sample.VoltageHundredths)
			: FormatCurrent(sample.CurrentThousandths);
		return WriteTextAsync(
			$"{FormatTime(sample.Elapsed)};{value};\n",
			cancellationToken);
	}

	public ValueTask WriteAsync(
		DualMeasurement measurement,
		MeasurementExportKind kind,
		CancellationToken cancellationToken)
	{
		string line=kind == MeasurementExportKind.Voltage
			? FormatDualVoltage(measurement)
			: FormatDualCurrent(measurement);
		return WriteTextAsync(line,cancellationToken);
	}

	public ValueTask WriteMissingAsync(
		TimeSpan elapsed,
		MeasurementLayout layout,
		MeasurementExportKind kind,
		CancellationToken cancellationToken)
	{
		int valueCount=GetValueCount(layout,kind);
		return WriteTextAsync(
			FormatTime(elapsed)+";"+new string(';',valueCount)+"\n",
			cancellationToken);
	}

	private static (string Labels,string Units) GetHeader(
		MeasurementLayout layout,
		MeasurementExportKind kind)
	{
		return (layout,kind) switch
		{
			(MeasurementLayout.Single,MeasurementExportKind.Voltage)=>
				("Time;Voltage;","[s];[V];"),
			(MeasurementLayout.Single,MeasurementExportKind.Current)=>
				("Time;Current;","[s];[A];"),
			(MeasurementLayout.Series,MeasurementExportKind.Voltage)=>
				("Time;Voltage 1;Voltage 2;Voltage total;",
				"[s];[V];[V];[V];"),
			(MeasurementLayout.Series,MeasurementExportKind.Current)=>
				("Time;Current 1;Current 2;","[s];[A];[A];"),
			(MeasurementLayout.Parallel,MeasurementExportKind.Voltage)=>
				("Time;Voltage 1;Voltage 2;","[s];[V];[V];"),
			(MeasurementLayout.Parallel,MeasurementExportKind.Current)=>
				("Time;Current 1;Current 2;Current total;",
				"[s];[A];[A];[A];"),
			(MeasurementLayout.Symmetric,MeasurementExportKind.Voltage)=>
				("Time;Voltage -;Voltage +;","[s];[V];[V];"),
			(MeasurementLayout.Symmetric,MeasurementExportKind.Current)=>
				("Time;Current -;Current +;","[s];[A];[A];"),
			_=>throw new ArgumentOutOfRangeException(nameof(layout))
		};
	}

	private static string FormatDualVoltage(DualMeasurement measurement)
	{
		string time=FormatTime(MaxElapsed(measurement));
		return measurement.Mode switch
		{
			DualMode.Series=>
				$"{time};{FormatVoltage(measurement.First.VoltageHundredths)};"+
				$"{FormatVoltage(measurement.Second.VoltageHundredths)};"+
				$"{FormatVoltage(measurement.VoltageHundredths)};\n",
			DualMode.Parallel=>
				$"{time};{FormatVoltage(measurement.First.VoltageHundredths)};"+
				$"{FormatVoltage(measurement.Second.VoltageHundredths)};\n",
			DualMode.Symmetric=>
				$"{time};{FormatVoltage(measurement.FirstSignedVoltageHundredths)};"+
				$"{FormatVoltage(measurement.SecondSignedVoltageHundredths)};\n",
			_=>throw new ArgumentOutOfRangeException(nameof(measurement))
		};
	}

	private static string FormatDualCurrent(DualMeasurement measurement)
	{
		string time=FormatTime(MaxElapsed(measurement));
		return measurement.Mode switch
		{
			DualMode.Series=>
				$"{time};{FormatCurrent(measurement.First.CurrentThousandths)};"+
				$"{FormatCurrent(measurement.Second.CurrentThousandths)};\n",
			DualMode.Parallel=>
				$"{time};{FormatCurrent(measurement.First.CurrentThousandths)};"+
				$"{FormatCurrent(measurement.Second.CurrentThousandths)};"+
				$"{FormatCurrent(measurement.CurrentThousandths)};\n",
			DualMode.Symmetric=>
				$"{time};{FormatCurrent(measurement.FirstSignedCurrentThousandths)};"+
				$"{FormatCurrent(measurement.SecondSignedCurrentThousandths)};\n",
			_=>throw new ArgumentOutOfRangeException(nameof(measurement))
		};
	}

	private static int GetValueCount(
		MeasurementLayout layout,
		MeasurementExportKind kind)
	{
		return (layout,kind) switch
		{
			(MeasurementLayout.Single,_)=>1,
			(MeasurementLayout.Series,MeasurementExportKind.Voltage)=>3,
			(MeasurementLayout.Series,MeasurementExportKind.Current)=>2,
			(MeasurementLayout.Parallel,MeasurementExportKind.Voltage)=>2,
			(MeasurementLayout.Parallel,MeasurementExportKind.Current)=>3,
			(MeasurementLayout.Symmetric,_)=>2,
			_=>throw new ArgumentOutOfRangeException(nameof(layout))
		};
	}

	private static TimeSpan MaxElapsed(DualMeasurement measurement)
	{
		return measurement.First.Elapsed >= measurement.Second.Elapsed
			? measurement.First.Elapsed
			: measurement.Second.Elapsed;
	}

	private static string FormatTime(TimeSpan elapsed)
	{
		return elapsed.TotalSeconds.ToString("0.000",CultureInfo.InvariantCulture);
	}

	private static string FormatVoltage(int hundredths)
	{
		return (hundredths/100m).ToString("0.00",CultureInfo.InvariantCulture);
	}

	private static string FormatCurrent(int thousandths)
	{
		return (thousandths/1000m).ToString("0.000",CultureInfo.InvariantCulture);
	}

	private async ValueTask WriteTextAsync(
		string value,
		CancellationToken cancellationToken)
	{
		await output.WriteAsync(value.AsMemory(),cancellationToken).ConfigureAwait(false);
	}
}
