using NowPlaying.Services;

namespace NowPlaying.Tests;

public sealed class MediaManagerServiceTests
{
    [Fact]
    public async Task Dispose_DoesNotInterruptReleaseOfAnActiveRefresh()
    {
        var gate = new RefreshGate();

        Assert.True(await gate.WaitAsync(TimeSpan.Zero));

        gate.Dispose();

        gate.Release();
        Assert.False(await gate.WaitAsync(TimeSpan.Zero));
    }
}