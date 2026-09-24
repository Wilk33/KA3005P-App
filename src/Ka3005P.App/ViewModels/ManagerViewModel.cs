using Ka3005P.App.Infrastructure;
using Ka3005P.App.Services;
using Ka3005P.Core.Configuration;

namespace Ka3005P.App.ViewModels;

public sealed class ManagerViewModel : ObservableObject
{
	private readonly IWindowService windows;
	private readonly ISettingsStore settingsStore;
	private AppSettings settings=new();
	private string? exportDirectory;
	private string? errorMessage;

	public ManagerViewModel(
		IWindowService windows,
		ISettingsStore settingsStore)
	{
		ArgumentNullException.ThrowIfNull(windows);
		ArgumentNullException.ThrowIfNull(settingsStore);
		this.windows=windows;
		this.settingsStore=settingsStore;
		OpenSingleCommand=new RelayCommand(_=>windows.Open(WindowKind.Single));
		OpenTwoSinglesCommand=new RelayCommand(_=>
		{
			windows.Open(WindowKind.Single);
			windows.Open(WindowKind.Single);
		});
		OpenDualCommand=new RelayCommand(_=>windows.Open(WindowKind.Dual));
		ChooseExportFolderCommand=new AsyncRelayCommand(
			ChooseExportFolderAsync,
			errorHandler:exception=>ErrorMessage=exception.Message);
	}

	public RelayCommand OpenSingleCommand { get; }
	public RelayCommand OpenTwoSinglesCommand { get; }
	public RelayCommand OpenDualCommand { get; }
	public AsyncRelayCommand ChooseExportFolderCommand { get; }

	public string? ExportDirectory
	{
		get => exportDirectory;
		private set => SetProperty(ref exportDirectory,value);
	}

	public string? ErrorMessage
	{
		get => errorMessage;
		private set => SetProperty(ref errorMessage,value);
	}

	public async Task InitializeAsync(CancellationToken cancellationToken)
	{
		settings=await settingsStore.LoadAsync(cancellationToken);
		ExportDirectory=settings.ExportDirectory;
	}

	private async Task ChooseExportFolderAsync(object? parameter)
	{
		string? chosen=await windows.ChooseExportFolderAsync(
			ExportDirectory,
			CancellationToken.None);
		if(chosen is null)
		{
			return;
		}

		ExportDirectory=chosen;
		settings=settings with { ExportDirectory=chosen };
		await settingsStore.SaveAsync(settings,CancellationToken.None);
	}
}
