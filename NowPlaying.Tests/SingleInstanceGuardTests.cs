using NowPlaying.Services;

namespace NowPlaying.Tests;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void NonOwnerCanDisposeWithoutReleasingAnotherInstancesMutex()
    {
        string name = $"Local\\NowPlaying.Tests.{Guid.NewGuid():N}";
        using var owner = new SingleInstanceGuard(name);
        var nonOwner = new SingleInstanceGuard(name);

        Assert.True(owner.IsOwner);
        Assert.False(nonOwner.IsOwner);

        nonOwner.Dispose();
        nonOwner.Dispose();
    }

    [Fact]
    public void OwnerReleasesMutexOnDispose()
    {
        string name = $"Local\\NowPlaying.Tests.{Guid.NewGuid():N}";
        var owner = new SingleInstanceGuard(name);

        Assert.True(owner.IsOwner);
        owner.Dispose();

        using var nextOwner = new SingleInstanceGuard(name);
        Assert.True(nextOwner.IsOwner);
    }
}