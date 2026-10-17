using System.Reflection;
using System.Threading;
using NowPlaying.Services;

namespace NowPlaying.Tests;

public sealed class MediaManagerServiceTests
{
    [Fact]
    public void Dispose_DoesNotDisposeRefreshLockHeldByAnInFlightRefresh()
    {
        var service = new MediaManagerService();
        var refreshLock = (SemaphoreSlim)typeof(MediaManagerService)
            .GetField("_refreshLock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service)!;

        Assert.True(refreshLock.Wait(0));

        service.Dispose();

        refreshLock.Release();
    }
}