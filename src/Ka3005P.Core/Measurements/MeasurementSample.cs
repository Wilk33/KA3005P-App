namespace Ka3005P.Core.Measurements;

public readonly record struct MeasurementSample(
	DateTimeOffset RecordedAt,
	long Timestamp,
	int VoltageHundredths,
	int CurrentThousandths);
