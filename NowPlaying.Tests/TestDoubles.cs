using NowPlaying.Models;
using NowPlaying.Services;

namespace NowPlaying.Tests;

internal sealed class FakeMediaManagerService : IMediaManagerService
{
    public event EventHandler<IReadOnlyList<MediaSessionModel>>? SessionsUpdated;

    public IReadOnlyList<MediaSessionModel> CurrentSessions { get; private set; } = Array.Empty<MediaSessionModel>();
    public string? LastCommand { get; private set; }
    public string? LastSessionId { get; private set; }
    public int RefreshCount { get; private set; }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task RefreshAsync()
    {
        RefreshCount++;
        return Task.CompletedTask;
    }

    public Task<bool> PlayAsync(string sessionId) => RecordCommand("Play", sessionId);
    public Task<bool> PauseAsync(string sessionId) => RecordCommand("Pause", sessionId);
    public Task<bool> TogglePlayPauseAsync(string sessionId) => RecordCommand("Toggle", sessionId);
    public Task<bool> SkipNextAsync(string sessionId) => RecordCommand("Next", sessionId);
    public Task<bool> SkipPreviousAsync(string sessionId) => RecordCommand("Previous", sessionId);

    public void PublishSessions(params MediaSessionModel[] sessions)
    {
        CurrentSessions = sessions;
        SessionsUpdated?.Invoke(this, CurrentSessions);
    }

    private Task<bool> RecordCommand(string command, string sessionId)
    {
        LastCommand = command;
        LastSessionId = sessionId;
        return Task.FromResult(true);
    }
}

internal sealed class FakeWindowFocusService : IWindowFocusService
{
    public string? LastFocusedApp { get; private set; }

    public bool FocusApplication(string sourceAppUserModelId)
    {
        LastFocusedApp = sourceAppUserModelId;
        return true;
    }
}

internal static class TestMediaSession
{
    public static MediaSessionModel Create(
        string id,
        string? title = null,
        bool isPlaying = false,
        TimeSpan? position = null,
        TimeSpan? duration = null) => new()
    {
        Id = id,
        SourceAppUserModelId = $"{id}.app",
        AppName = id,
        Title = title ?? $"Title for {id}",
        IsPlaying = isPlaying,
        Position = position ?? TimeSpan.Zero,
        Duration = duration ?? TimeSpan.Zero
    };
}