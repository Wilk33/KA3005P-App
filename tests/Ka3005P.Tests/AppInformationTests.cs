using Ka3005P.App;

namespace Ka3005P.Tests;

public sealed class AppInformationTests
{
	[Fact]
	public void WindowTitle_ContainsApplicationVersionAndDemoMarker()
	{
		Assert.Equal("Korad v0.1.5",AppInformation.GetWindowTitle(false));
		Assert.Equal("Korad v0.1.5 - DEMO",AppInformation.GetWindowTitle(true));
	}

	[Fact]
	public void AuthorText_ContainsRequestedIdentity()
	{
		Assert.Contains("Mateusz Skipor",AppInformation.AuthorText);
		Assert.Contains("mskiporsklep@op.pl",AppInformation.AuthorText);
		Assert.Contains("Inżynier technik elektroniki",AppInformation.AuthorText);
	}

	[Fact]
	public void EmbeddedLicense_ContainsPolyFormLicense()
	{
		string license=AppInformation.LoadLicenseText();

		Assert.Contains("PolyForm Noncommercial License 1.0.0",license);
	}
}
