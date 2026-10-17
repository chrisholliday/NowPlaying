using NowPlaying.Services;

namespace NowPlaying.Tests;

public sealed class MediaSessionModelFactoryTests
{
    [Fact]
    public void CreateOrdered_SortsPlayingSessionsFirstAndOmitsClosedSessions()
    {
        var snapshots = new MediaSessionSnapshot?[]
        {
            CreateSnapshot("paused-z", "Zulu"),
            CreateSnapshot("playing-z", "Zulu", isPlaying: true),
            CreateSnapshot("closed", "Closed", isClosed: true),
            CreateSnapshot("paused-a", "Alpha")
        };

        var models = MediaSessionModelFactory.CreateOrdered(snapshots);

        Assert.Equal(["playing-z", "paused-a", "paused-z"], models.Select(model => model.Id));
    }

    [Fact]
    public void Create_UsesAppNameWhenMetadataIsEmptyAndPreservesPlaybackData()
    {
        var model = MediaSessionModelFactory.Create(CreateSnapshot(
            "spotify", "Spotify", position: TimeSpan.FromSeconds(40), endTime: TimeSpan.FromSeconds(180)));

        Assert.NotNull(model);
        Assert.Equal("Spotify", model.Title);
        Assert.Equal(string.Empty, model.Artist);
        Assert.Equal(TimeSpan.FromSeconds(40), model.Position);
        Assert.Equal(TimeSpan.FromSeconds(180), model.Duration);
        Assert.True(model.CanPlay);
        Assert.True(model.CanPause);
        Assert.False(model.CanSkipNext);
        Assert.False(model.CanSkipPrevious);
    }

    [Fact]
    public void Create_UsesAvailableTrackMetadataAndExplicitControls()
    {
        var snapshot = CreateSnapshot("player", "Player") with
        {
            Title = "Track",
            Artist = "Artist",
            Album = "Album",
            CanPlay = false,
            CanPause = false,
            CanSkipNext = true,
            CanSkipPrevious = true
        };

        var model = MediaSessionModelFactory.Create(snapshot);

        Assert.NotNull(model);
        Assert.Equal("Track", model.Title);
        Assert.Equal("Artist", model.Artist);
        Assert.Equal("Album", model.Album);
        Assert.False(model.CanPlay);
        Assert.False(model.CanPause);
        Assert.True(model.CanSkipNext);
        Assert.True(model.CanSkipPrevious);
    }

    private static MediaSessionSnapshot CreateSnapshot(
        string id,
        string appName,
        bool isPlaying = false,
        bool isClosed = false,
        TimeSpan? position = null,
        TimeSpan? endTime = null) => new()
    {
        Id = id,
        SourceAppUserModelId = $"{id}.app",
        AppName = appName,
        IsPlaying = isPlaying,
        IsClosed = isClosed,
        Position = position ?? TimeSpan.Zero,
        EndTime = endTime ?? TimeSpan.Zero
    };
}