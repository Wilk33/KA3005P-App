using System.ComponentModel;
using System.Windows;
using Ka3005P.App.Services;
using Ka3005P.App.ViewModels;

namespace Ka3005P.App.Views;

public partial class DualSupplyWindow : Window
{
	private bool closeCompleted;

	public DualSupplyWindow()
	{
		InitializeComponent();
	}

	private void OpenChartClick(object sender,RoutedEventArgs eventArgs)
	{
		if(DataContext is not DualSupplyViewModel viewModel)
		{
			return;
		}
		ChartWindow window=new()
		{
			Owner=this,
			DataContext=viewModel.CreateChartViewModel(new FileDialogService())
		};
		window.Show();
	}

	private async void SaveVoltageClick(object sender,RoutedEventArgs eventArgs)
	{
		await SaveAsync(true);
	}

	private async void SaveCurrentClick(object sender,RoutedEventArgs eventArgs)
	{
		await SaveAsync(false);
	}

	private async Task SaveAsync(bool voltage)
	{
		if(DataContext is not DualSupplyViewModel viewModel)
		{
			return;
		}
		using ChartViewModel chart=viewModel.CreateChartViewModel(
			new FileDialogService());
		if(voltage)
		{
			await chart.SaveVoltageCommand.ExecuteAsync(null);
		}
		else
		{
			await chart.SaveCurrentCommand.ExecuteAsync(null);
		}
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		if(closeCompleted || DataContext is not DualSupplyViewModel viewModel)
		{
			base.OnClosing(e);
			return;
		}

		e.Cancel=true;
		await viewModel.CloseAsync(CancellationToken.None);
		closeCompleted=true;
		Close();
	}
}
