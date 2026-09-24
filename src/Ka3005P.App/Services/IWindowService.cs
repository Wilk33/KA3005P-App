namespace Ka3005P.App.Services;

public enum WindowKind
{
	Single,
	Dual
}

public sealed record WindowRequest(WindowKind Kind);

public interface IWindowService
{
	void Open(WindowKind kind);
	ValueTask<string?> ChooseExportFolderAsync(
		string? initialDirectory,
		CancellationToken cancellationToken);
}
