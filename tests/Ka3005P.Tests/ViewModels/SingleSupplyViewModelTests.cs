using Ka3005P.App.Services;
using Ka3005P.App.ViewModels;
using Ka3005P.Core.Configuration;
using Ka3005P.Core.Measurements;
using Ka3005P.Core.Sessions;
using Ka3005P.Tests.Fakes;

namespace Ka3005P.Tests.ViewModels;

public sealed class SingleSupplyViewModelTests
{
	[Fact]
	public void IncrementVoltage_UpdatesUiAndQueuesRequestSynchronously()
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel viewModel=new(session){VoltageText="12,00"};

		viewModel.IncrementVoltageCommand.Execute(null);

		Assert.Equal("12,01",viewModel.VoltageText);
		Assert.Equal(1201,session.RequestedVoltage?.Hundredths);
		Assert.False(viewModel.HasValidationError);
	}

	[Theory]
	[InlineData("12,34",1234)]
	[InlineData("12.34",1234)]
	public void CommitVoltage_AcceptsCommaAndDot(string text,int expected)
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel viewModel=new(session){VoltageText=text};

		viewModel.CommitVoltageCommand.Execute(null);

		Assert.Equal(expected,session.RequestedVoltage?.Hundredths);
		Assert.Equal("12,34",viewModel.VoltageText);
	}

	[Theory]
	[InlineData("")]
	[InlineData("-1")]
	[InlineData("31,01")]
	[InlineData("abc")]
	public void CommitVoltage_InvalidValueDoesNotQueueRequest(string text)
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel viewModel=new(session){VoltageText=text};

		viewModel.CommitVoltageCommand.Execute(null);

		Assert.Null(session.RequestedVoltage);
		Assert.True(viewModel.HasValidationError);
	}

	[Theory]
	[InlineData("5,100",5100,false)]
	[InlineData("5.100",5100,false)]
	[InlineData("5,101",0,true)]
	[InlineData("",0,true)]
	public void CommitCurrent_ValidatesDeviceRange(
		string text,
		int expected,
		bool invalid)
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel viewModel=new(session){CurrentText=text};

		viewModel.CommitCurrentCommand.Execute(null);

		Assert.Equal(invalid,viewModel.HasValidationError);
		if(invalid)
		{
			Assert.Null(session.RequestedCurrent);
		}
		else
		{
			Assert.Equal(expected,session.RequestedCurrent?.Thousandths);
		}
	}

	[Fact]
	public void Measurement_UpdatesOutputWithoutChangingSetpoints()
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel viewModel=new(session){VoltageText="12,00"};
		viewModel.CommitVoltageCommand.Execute(null);

		session.PublishMeasurement(
			new MeasurementSample(TimeSpan.FromSeconds(1),0,0));

		Assert.Equal("12,00",viewModel.VoltageText);
		Assert.Equal("0,00 V",viewModel.MeasuredVoltageText);
		Assert.Equal("0,000 A",viewModel.MeasuredCurrentText);
		Assert.False(viewModel.IsResistanceVisible);
	}

	[Fact]
	public void Measurement_WithCurrentShowsResistance()
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel viewModel=new(session);

		session.PublishMeasurement(
			new MeasurementSample(TimeSpan.Zero,1200,1000));

		Assert.True(viewModel.IsResistanceVisible);
		Assert.Equal("12 Ω",viewModel.ResistanceText);
	}

	[Fact]
	public async Task Connect_RejectsPortAlreadyLeasedByAnotherWindow()
	{
		PortLeaseRegistry leases=new();
		FakeSingleSessionFactory factory=new();
		SingleSupplyViewModel first=new(leases,factory){PortName="COM5"};
		SingleSupplyViewModel second=new(leases,factory){PortName="com5"};
		await first.ConnectCommand.ExecuteAsync(null);

		await second.ConnectCommand.ExecuteAsync(null);

		Assert.True(first.IsConnected);
		Assert.False(second.IsConnected);
		Assert.Contains("używany",second.ErrorMessage,StringComparison.OrdinalIgnoreCase);
		await first.CloseAsync(CancellationToken.None);
		await second.CloseAsync(CancellationToken.None);
	}

	[Fact]
	public async Task CloseAsync_SendsOffStopsSessionAndReleasesPort()
	{
		PortLeaseRegistry leases=new();
		FakeSingleSessionFactory factory=new();
		SingleSupplyViewModel viewModel=new(leases,factory){PortName="COM7"};
		await viewModel.ConnectCommand.ExecuteAsync(null);
		FakePowerSupplySession session=Assert.IsType<FakePowerSupplySession>(factory.LastSession);

		await viewModel.CloseAsync(CancellationToken.None);

		Assert.Contains(false,session.Outputs);
		Assert.True(session.StopRequested);
		Assert.True(leases.TryAcquire("COM7",out PortLease? lease));
		lease.Dispose();
	}

	private sealed class FakeSingleSessionFactory : ISingleSessionFactory
	{
		public IPowerSupplySession? LastSession { get; private set; }

		public ValueTask<IPowerSupplySession> CreateAsync(
			string portName,
			CancellationToken cancellationToken)
		{
			LastSession=new FakePowerSupplySession();
			return ValueTask.FromResult(LastSession);
		}
	}
}
