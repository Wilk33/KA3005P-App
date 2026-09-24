using System.IO;
using Microsoft.Win32;

namespace Ka3005P.App.Services;

public sealed class WindowService : IWindowService
{
	private readonly Action<WindowRequest> openWindow;

	public WindowService(Action<WindowRequest> openWindow)
	{
		ArgumentNullException.ThrowIfNull(openWindow);
		this.openWindow=openWindow;
	}

	public void Open(WindowKind kind)
	{
		openWindow(new WindowRequest(kind));
	}

	public ValueTask<string?> ChooseExportFolderAsync(
		string? initialDirectory,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		OpenFolderDialog dialog=new()
		{
			Title="Wybierz folder zapisu pomiarów",
			Multiselect=false
		};
		if(!string.IsNullOrWhiteSpace(initialDirectory) &&
			Directory.Exists(initialDirectory))
		{
			dialog.InitialDirectory=initialDirectory;
		}

		bool? result=dialog.ShowDialog();
		return ValueTask.FromResult(result == true ? dialog.FolderName : null);
	}
}
