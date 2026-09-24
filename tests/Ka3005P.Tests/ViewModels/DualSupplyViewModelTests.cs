using Ka3005P.App.Services;
using Ka3005P.App.ViewModels;
using Ka3005P.Core.Configuration;
using Ka3005P.Core.Dual;
using Ka3005P.Core.Measurements;
using Ka3005P.Core.Protocol;
using Ka3005P.Core.Sessions;
using Ka3005P.Tests.Fakes;

namespace Ka3005P.Tests.ViewModels;

public sealed class DualSupplyViewModelTests
{
	[Theory]
	[InlineData(DualMode.Series,"62,00","5,100")]
	[InlineData(DualMode.Parallel,"31,00","10,200")]
	[InlineData(DualMode.Symmetric,"31,00","5,100")]
	public void ChangeMode_UpdatesLimitsAndRevalidatesSetpoints(
		DualMode mode,
		string maxVoltage,
		string maxCurrent)
	{
		(DualSupplyViewModel viewModel,_,_)=CreateViewModel();

		viewModel.Mode=mode;

		Assert.Equal(maxVoltage,viewModel.MaximumVoltageText);
		Assert.Equal(maxCurrent,viewModel.MaximumCurrentText);
		Assert.True(viewModel.SetpointsAreValid);
	}

	[Fact]
	public async Task Connect_RejectsIdenticalPortNames()
	{
		DualSupplyViewModel viewModel=new(
			new PortLeaseRegistry(),
			new FakeSessionFactory())
		{
			FirstPortName="COM5",
			SecondPortName=" com5 "
		};

		await viewModel.ConnectCommand.ExecuteAsync(null);

		Assert.False(viewModel.IsConnected);
		Assert.Contains("różne",viewModel.ErrorMessage,StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Connect_PartialFailureIdentifiesFailingPortAndCleansUp()
	{
		PortLeaseRegistry leases=new();
		FakeSessionFactory factory=new(){FailPort="COM8"};
		DualSupplyViewModel viewModel=new(leases,factory)
		{
			FirstPortName="COM7",
			SecondPortName="COM8"
		};

		await viewModel.ConnectCommand.ExecuteAsync(null);

		Assert.False(viewModel.IsConnected);
		Assert.Contains("COM8",viewModel.ErrorMessage);
		Assert.True(leases.TryAcquire("COM7",out PortLease? first));
		Assert.True(leases.TryAcquire("COM8",out PortLease? second));
		first.Dispose();
		second.Dispose();
	}

	[Fact]
	public void RapidVoltageChanges_AreAcceptedSynchronously()
	{
		(DualSupplyViewModel viewModel,FakePowerSupplySession first,FakePowerSupplySession second)=
			CreateViewModel();

		for(int value=1200;value<=1250;value++)
		{
			viewModel.SetVoltageFromHundredths(value);
		}

		Assert.Equal("12,50",viewModel.VoltageText);
		Assert.Equal(625,first.RequestedVoltage?.Hundredths);
		Assert.Equal(625,second.RequestedVoltage?.Hundredths);
	}

	[Fact]
	public async Task ToggleOutput_UsesCommonControllerOperation()
	{
		(DualSupplyViewModel viewModel,FakePowerSupplySession first,FakePowerSupplySession second)=
			CreateViewModel();

		await viewModel.ToggleOutputCommand.ExecuteAsync(null);
		await viewModel.ToggleOutputCommand.ExecuteAsync(null);

		Assert.Equal([true,false],first.Outputs);
		Assert.Equal([true,false],second.Outputs);
		Assert.False(viewModel.IsOutputOn);
	}

	[Fact]
	public void Measurements_ShowLogicalAndSeparateDeviceValues()
	{
		(DualSupplyViewModel viewModel,FakePowerSupplySession first,FakePowerSupplySession second)=
			CreateViewModel();
		viewModel.Mode=DualMode.Series;

		first.PublishMeasurement(new MeasurementSample(TimeSpan.Zero,1200,400));
		second.PublishMeasurement(new MeasurementSample(TimeSpan.Zero,1100,350));

		Assert.Equal("23,00 V",viewModel.MeasuredVoltageText);
		Assert.Equal("0,400 A",viewModel.MeasuredCurrentText);
		Assert.Equal("12,00 V / 0,400 A",viewModel.FirstMeasurementText);
		Assert.Equal("11,00 V / 0,350 A",viewModel.SecondMeasurementText);
	}

	private static (
		DualSupplyViewModel ViewModel,
		FakePowerSupplySession First,
		FakePowerSupplySession Second) CreateViewModel()
	{
		FakePowerSupplySession first=new();
		FakePowerSupplySession second=new();
		DualPowerSupplyController controller=
			new(first,second,DualMode.Series);
		return (new DualSupplyViewModel(controller),first,second);
	}

	private sealed class FakeSessionFactory : ISingleSessionFactory
	{
		public string? FailPort { get; init; }

		public ValueTask<IPowerSupplySession> CreateAsync(
			string portName,
			CancellationToken cancellationToken)
		{
			if(string.Equals(portName,FailPort,StringComparison.OrdinalIgnoreCase))
			{
				throw new IOException(portName);
			}
			return ValueTask.FromResult<IPowerSupplySession>(
				new FakePowerSupplySession());
		}
	}
}
