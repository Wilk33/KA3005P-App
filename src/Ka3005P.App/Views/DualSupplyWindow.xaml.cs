using System.ComponentModel;
using System.Windows;
using Ka3005P.App.ViewModels;

namespace Ka3005P.App.Views;

public partial class DualSupplyWindow : Window
{
	private bool closeCompleted;

	public DualSupplyWindow()
	{
		InitializeComponent();
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
