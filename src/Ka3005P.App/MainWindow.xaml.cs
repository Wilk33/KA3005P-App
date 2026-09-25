using System.ComponentModel;
using System.Windows;
using Ka3005P.App.Infrastructure;
using Ka3005P.App.Services;
using Ka3005P.App.ViewModels;
using Ka3005P.App.Views;
using Ka3005P.Core.Measurements;

namespace Ka3005P.App;

public partial class MainWindow : Window
{
	private ChartWindow? chartWindow;
	private bool closeCompleted;

	public MainWindow()
	{
		InitializeComponent();
	}

	private async void SelectSingleClick(object sender,RoutedEventArgs eventArgs)
	{
		await SwitchModeAsync(ApplicationMode.Single);
	}

	private async void SelectDualClick(object sender,RoutedEventArgs eventArgs)
	{
		await SwitchModeAsync(ApplicationMode.Dual);
	}

	private async Task SwitchModeAsync(ApplicationMode mode)
	{
		if(DataContext is not MainWindowViewModel viewModel)
		{
			return;
		}
		CloseChart();
		AsyncRelayCommand command=mode == ApplicationMode.Single
			? viewModel.SelectSingleCommand
			: viewModel.SelectDualCommand;
		await command.ExecuteAsync(null);
	}

	private void OpenChartClick(object sender,RoutedEventArgs eventArgs)
	{
		if(DataContext is not MainWindowViewModel viewModel)
		{
			return;
		}
		if(chartWindow is not null)
		{
			chartWindow.Activate();
			return;
		}
		chartWindow=new ChartWindow
		{
			Owner=this,
			DataContext=viewModel.CurrentMode.CreateChartViewModel(
				new FileDialogService())
		};
		chartWindow.Closed+=(_,_)=>chartWindow=null;
		chartWindow.Show();
	}

	private async void SaveVoltageClick(object sender,RoutedEventArgs eventArgs)
	{
		await SaveAsync(MeasurementExportKind.Voltage);
	}

	private async void SaveCurrentClick(object sender,RoutedEventArgs eventArgs)
	{
		await SaveAsync(MeasurementExportKind.Current);
	}

	private async Task SaveAsync(MeasurementExportKind kind)
	{
		if(DataContext is not MainWindowViewModel viewModel)
		{
			return;
		}
		FileDialogService dialog=new();
		string name=kind == MeasurementExportKind.Voltage
			? "napiecie.csv"
			: "prad.csv";
		string? path=await dialog.ChooseSavePathAsync(
			name,
			CancellationToken.None);
		if(path is not null)
		{
			await viewModel.CurrentMode.ExportAsync(
				kind,
				path,
				CancellationToken.None);
		}
	}

	private void CloseChart()
	{
		chartWindow?.Close();
		chartWindow=null;
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		if(closeCompleted || DataContext is not MainWindowViewModel viewModel)
		{
			base.OnClosing(e);
			return;
		}
		e.Cancel=true;
		CloseChart();
		await viewModel.DisposeAsync();
		closeCompleted=true;
		if(!Dispatcher.HasShutdownStarted)
		{
			await Dispatcher.InvokeAsync(Close);
		}
	}
}
