using System.ComponentModel;
using System.Windows;
using Ka3005P.App.ViewModels;

namespace Ka3005P.App.Views;

public partial class SingleSupplyWindow : Window
{
	private bool closeCompleted;

	public SingleSupplyWindow()
	{
		InitializeComponent();
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		if(closeCompleted || DataContext is not SingleSupplyViewModel viewModel)
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
