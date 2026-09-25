using System.IO;
using System.Windows;
using Ka3005P.App.Demo;
using Ka3005P.App.Services;
using Ka3005P.App.ViewModels;
using Ka3005P.Core.Configuration;

namespace Ka3005P.App;

public partial class App
{
	private readonly PortLeaseRegistry portLeases=new();
	private ISingleSessionFactory singleSessionFactory=
		new SerialSingleSessionFactory(TimeProvider.System);

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		bool demoMode=e.Args.Any(argument=>
			string.Equals(argument,"--demo",StringComparison.OrdinalIgnoreCase));
		if(demoMode)
		{
			singleSessionFactory=new DemoSingleSessionFactory(TimeProvider.System);
		}
		string dataDirectory=Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"KA3005P App");
		ISettingsStore settings=new JsonSettingsStore(
			Path.Combine(dataDirectory,"settings.json"));
		ISerialPortCatalog catalog=demoMode
			? new DemoSerialPortCatalog()
			: new SystemSerialPortCatalog();
		SerialPortMonitor portMonitor=new(
			catalog,
			TimeSpan.FromSeconds(1));
		SupplyModeFactory modes=new(portLeases,singleSessionFactory);
		MainWindowViewModel viewModel=new(modes,portMonitor,settings);
		await viewModel.InitializeAsync(CancellationToken.None);
		MainWindow window=new()
		{
			DataContext=viewModel,
			Title=demoMode ? "Korad - DEMO" : "Korad"
		};
		MainWindow=window;
		window.Show();
	}
}
