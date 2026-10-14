using NowPlaying.ViewModels;

namespace NowPlaying.Tests;

public sealed class MediaCardViewModelTests
{
    [Theory]
    [InlineData(30, 120, 25)]
    [InlineData(150, 120, 100)]
    [InlineData(-10, 120, 0)]
    [InlineData(10, 0, 0)]
    public void ProgressPercentage_IsBoundedByDuration(double positionSeconds, double durationSeconds, double expected)
    {
        var viewModel = CreateViewModel(TestMediaSession.Create(
            "player",
            position: TimeSpan.FromSeconds(positionSeconds),
            duration: TimeSpan.FromSeconds(durationSeconds)));

        Assert.Equal(expected, viewModel.ProgressPercentage);
    }

    [Theory]
    [InlineData(83, "1:23")]
    [InlineData(3723, "1:02:03")]
    public void PositionText_UsesReadableTimeFormat(double seconds, string expected)
    {
        var viewModel = CreateViewModel(TestMediaSession.Create(
            "player",
            position: TimeSpan.FromSeconds(seconds)));

        Assert.Equal(expected, viewModel.PositionText);
    }

    [Fact]
    public void EmptyTitle_FallsBackToAppName()
    {
        var model = TestMediaSession.Create("Spotify", title: "   ");

        var viewModel = CreateViewModel(model);

        Assert.Equal("Spotify", viewModel.Title);
    }

    [Fact]
    public async Task Commands_AreForwardedToTheMatchingSession()
    {
        var mediaService = new FakeMediaManagerService();
        var focusService = new FakeWindowFocusService();
        var viewModel = new MediaCardViewModel(
            TestMediaSession.Create("browser-session"),
            mediaService,
            focusService);

        await viewModel.TogglePlayPauseCommand.ExecuteAsync(null);
        Assert.Equal("Toggle", mediaService.LastCommand);
        Assert.Equal("browser-session", mediaService.LastSessionId);

        await viewModel.SkipNextCommand.ExecuteAsync(null);
        Assert.Equal("Next", mediaService.LastCommand);

        await viewModel.SkipPreviousCommand.ExecuteAsync(null);
        Assert.Equal("Previous", mediaService.LastCommand);

        viewModel.FocusAppCommand.Execute(null);
        Assert.Equal("browser-session.app", focusService.LastFocusedApp);
    }

    private static MediaCardViewModel CreateViewModel(Models.MediaSessionModel model) =>
        new(model, new FakeMediaManagerService(), new FakeWindowFocusService());
}