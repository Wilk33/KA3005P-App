using Ka3005P.App.Services;
using Ka3005P.App.ViewModels;
using Ka3005P.Core.Configuration;

namespace Ka3005P.Tests.ViewModels;

public sealed class ManagerViewModelTests
{
	[Fact]
	public void OpenTwoSingles_CreatesTwoIndependentWindowRequests()
	{
		FakeWindowService windows=new();
		ManagerViewModel viewModel=new(windows,new FakeSettingsStore());

		viewModel.OpenTwoSinglesCommand.Execute(null);

		Assert.Collection(
			windows.Requests,
			request=>Assert.Equal(WindowKind.Single,request.Kind),
			request=>Assert.Equal(WindowKind.Single,request.Kind));
	}

	[Fact]
	public async Task ChooseExportFolder_SavesSelectedDirectory()
	{
		FakeWindowService windows=new(){ChosenFolder="D:\\Pomiary"};
		FakeSettingsStore settings=new();
		ManagerViewModel viewModel=new(windows,settings);
		await viewModel.InitializeAsync(CancellationToken.None);

		await viewModel.ChooseExportFolderCommand.ExecuteAsync(null);

		Assert.Equal("D:\\Pomiary",viewModel.ExportDirectory);
		Assert.Equal("D:\\Pomiary",settings.Saved?.ExportDirectory);
	}

	[Fact]
	public void OpeningAnotherWindow_DoesNotCloseExistingRequests()
	{
		FakeWindowService windows=new();
		ManagerViewModel viewModel=new(windows,new FakeSettingsStore());
		viewModel.OpenSingleCommand.Execute(null);

		viewModel.OpenDualCommand.Execute(null);

		Assert.Equal(
			[WindowKind.Single,WindowKind.Dual],
			windows.Requests.Select(request=>request.Kind));
	}

	private sealed class FakeWindowService : IWindowService
	{
		public List<WindowRequest> Requests { get; }=[];
		public string? ChosenFolder { get; init; }

		public void Open(WindowKind kind)
		{
			Requests.Add(new WindowRequest(kind));
		}

		public ValueTask<string?> ChooseExportFolderAsync(
			string? initialDirectory,
			CancellationToken cancellationToken)
		{
			return ValueTask.FromResult(ChosenFolder);
		}
	}

	private sealed class FakeSettingsStore : ISettingsStore
	{
		public AppSettings Loaded { get; init; }=new();
		public AppSettings? Saved { get; private set; }

		public ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken)
		{
			return ValueTask.FromResult(Loaded);
		}

		public ValueTask SaveAsync(
			AppSettings settings,
			CancellationToken cancellationToken)
		{
			Saved=settings;
			return ValueTask.CompletedTask;
		}
	}
}
