using Ka3005P.App.ViewModels;
using Ka3005P.Core.Measurements;
using Ka3005P.Tests.Fakes;

namespace Ka3005P.Tests.ViewModels;

public sealed class ChartViewModelTests
{
	[Fact]
	public void AddSample_KeepsLatestFiftyAndScalesAxis()
	{
		FakeOutputController output=new();
		using ChartViewModel viewModel=new(output);
		for(int index=0;index<60;index++)
		{
			viewModel.AddSample(
				TimeSpan.FromMilliseconds(index*100),
				index);
		}

		Assert.Equal(50,viewModel.Points.Count);
		Assert.Equal(10,viewModel.Points[0].Value);
		Assert.Equal(59,viewModel.Points[^1].Value);
		Assert.Equal(9.5,viewModel.MinimumY);
		Assert.Equal(59.5,viewModel.MaximumY);
	}

	[Fact]
	public void AddSample_ConstantAndNearZeroValuesKeepVisibleRange()
	{
		using ChartViewModel constant=new(new FakeOutputController());
		constant.AddSample(TimeSpan.Zero,2);
		constant.AddSample(TimeSpan.FromSeconds(1),2);
		Assert.Equal(1.5,constant.MinimumY);
		Assert.Equal(2.5,constant.MaximumY);

		using ChartViewModel nearZero=new(new FakeOutputController());
		nearZero.AddSample(TimeSpan.Zero,0.1);
		nearZero.AddSample(TimeSpan.FromSeconds(1),0.2);
		Assert.Equal(0,nearZero.MinimumY);
		Assert.Equal(0.7,nearZero.MaximumY,6);
	}

	[Fact]
	public async Task ToggleOutput_UsesSameStateForMainAndChart()
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel main=new(session);
		using ChartViewModel chart=main.CreateChartViewModel(new FakeFileDialogService());

		await chart.ToggleOutputCommand.ExecuteAsync(null);

		Assert.True(main.IsOutputOn);
		Assert.True(chart.IsOutputOn);
		Assert.Equal([true],session.Outputs);
	}

	[Fact]
	public void ClosingAndReopeningChart_DoesNotRequestMeasurements()
	{
		FakePowerSupplySession session=new();
		SingleSupplyViewModel main=new(session);
		ChartViewModel first=main.CreateChartViewModel(new FakeFileDialogService());
		first.Dispose();
		using ChartViewModel second=main.CreateChartViewModel(new FakeFileDialogService());

		Assert.Equal(0,session.MeasurementRequests);
	}

	private sealed class FakeOutputController : IOutputController
	{
		public event EventHandler<bool>? OutputStateChanged;
		public bool IsOutputOn { get; private set; }

		public ValueTask SetOutputAsync(bool enabled,CancellationToken cancellationToken)
		{
			IsOutputOn=enabled;
			OutputStateChanged?.Invoke(this,enabled);
			return ValueTask.CompletedTask;
		}
	}
}
