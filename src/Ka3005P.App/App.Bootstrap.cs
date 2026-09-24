using System.IO;
using System.Windows;
using Ka3005P.App.Services;
using Ka3005P.App.ViewModels;
using Ka3005P.App.Views;
using Ka3005P.Core.Configuration;

namespace Ka3005P.App;

public partial class App
{
	private readonly PortLeaseRegistry portLeases=new();
	private readonly ISingleSessionFactory singleSessionFactory=
		new SerialSingleSessionFactory(TimeProvider.System);

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		string dataDirectory=Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"KA3005P App");
		ISettingsStore settings=new JsonSettingsStore(
			Path.Combine(dataDirectory,"settings.json"));
		WindowService windows=new(OpenRequestedWindow);
		ManagerViewModel viewModel=new(windows,settings);
		await viewModel.InitializeAsync(CancellationToken.None);
		ManagerWindow window=new()
		{
			DataContext=viewModel
		};
		MainWindow=window;
		window.Show();
	}

	private void OpenRequestedWindow(WindowRequest request)
	{
		if(request.Kind == WindowKind.Single)
		{
			SingleSupplyWindow window=new()
			{
				Owner=MainWindow,
				DataContext=new SingleSupplyViewModel(portLeases,singleSessionFactory)
			};
			window.Show();
			return;
		}

		DualSupplyWindow dualWindow=new()
		{
			Owner=MainWindow,
			DataContext=new DualSupplyViewModel(portLeases,singleSessionFactory)
		};
		dualWindow.Show();
	}
}
