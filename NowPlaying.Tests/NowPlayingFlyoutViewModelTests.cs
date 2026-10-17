using NowPlaying.ViewModels;

namespace NowPlaying.Tests;

public sealed class NowPlayingFlyoutViewModelTests
{
    [Fact]
    public void UpdateSessions_ShowsAllSourcesAndSelectsPlayingSource()
    {
        var viewModel = CreateViewModel();

        viewModel.UpdateSessions(
        [
            TestMediaSession.Create("spotify"),
            TestMediaSession.Create("browser", isPlaying: true)
        ]);

        Assert.Equal(2, viewModel.MediaCards.Count);
        Assert.True(viewModel.HasActiveMedia);
        Assert.True(viewModel.IsAnyPlaying);
        Assert.Equal("Now Playing (2)", viewModel.HeaderTitle);
        Assert.Equal("browser", viewModel.PrimarySession?.Id);
    }

    [Fact]
    public void UpdateSessions_PreservesExistingCardAndRemovesMissingSessions()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateSessions(
        [
            TestMediaSession.Create("spotify", title: "Old title"),
            TestMediaSession.Create("browser")
        ]);
        var originalCard = viewModel.MediaCards[0];

        viewModel.UpdateSessions([TestMediaSession.Create("spotify", title: "New title")]);

        var updatedCard = Assert.Single(viewModel.MediaCards);
        Assert.Same(originalCard, updatedCard);
        Assert.Equal("New title", updatedCard.Title);
        Assert.Equal("Now Playing", viewModel.HeaderTitle);
        Assert.False(viewModel.IsAnyPlaying);
    }

    [Fact]
    public void SessionUpdateEvent_UpdatesTheViewModel()
    {
        var mediaService = new FakeMediaManagerService();
        var viewModel = new NowPlayingFlyoutViewModel(mediaService, new FakeWindowFocusService());

        mediaService.PublishSessions(TestMediaSession.Create("player", isPlaying: true));

        Assert.Single(viewModel.MediaCards);
        Assert.True(viewModel.IsAnyPlaying);
    }

    [Fact]
    public void UpdateSessions_ReordersExistingCardsAndSwitchesPrimaryPlayer()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateSessions(
        [
            TestMediaSession.Create("spotify", isPlaying: true),
            TestMediaSession.Create("browser")
        ]);
        var spotifyCard = viewModel.MediaCards[0];
        var browserCard = viewModel.MediaCards[1];

        viewModel.UpdateSessions(
        [
            TestMediaSession.Create("browser", isPlaying: true),
            TestMediaSession.Create("spotify")
        ]);

        Assert.Same(browserCard, viewModel.MediaCards[0]);
        Assert.Same(spotifyCard, viewModel.MediaCards[1]);
        Assert.Same(browserCard, viewModel.PrimarySession);
        Assert.True(viewModel.IsAnyPlaying);
    }

    [Fact]
    public void UpdateSessions_WhenAllSessionsDisappear_ClearsActiveState()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateSessions([TestMediaSession.Create("player", isPlaying: true)]);

        viewModel.UpdateSessions([]);

        Assert.Empty(viewModel.MediaCards);
        Assert.Null(viewModel.PrimarySession);
        Assert.False(viewModel.HasActiveMedia);
        Assert.False(viewModel.IsAnyPlaying);
        Assert.Equal("Now Playing", viewModel.HeaderTitle);
    }

    [Fact]
    public async Task RefreshAsync_RequestsARefreshFromTheMediaService()
    {
        var mediaService = new FakeMediaManagerService();
        var viewModel = new NowPlayingFlyoutViewModel(mediaService, new FakeWindowFocusService());

        await viewModel.RefreshAsync();

        Assert.Equal(1, mediaService.RefreshCount);
    }

    private static NowPlayingFlyoutViewModel CreateViewModel() =>
        new(new FakeMediaManagerService(), new FakeWindowFocusService());
}