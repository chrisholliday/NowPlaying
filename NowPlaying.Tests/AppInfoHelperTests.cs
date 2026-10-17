using NowPlaying.Services;

namespace NowPlaying.Tests;

public sealed class AppInfoHelperTests
{
    [Fact]
    public void GetAppIcon_ReturnsNullWhenNoIconCanBeResolved()
    {
        var result = AppInfoHelper.GetAppIcon("NowPlaying.Tests.MissingIconApp.7E9A4C1B");

        Assert.Null(result);
        Assert.Null(AppInfoHelper.GetAppIcon("NowPlaying.Tests.MissingIconApp.7E9A4C1B"));
    }
}