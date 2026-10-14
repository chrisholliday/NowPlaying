using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NowPlaying.Models;

namespace NowPlaying.Services;

public interface IMediaManagerService
{
    event EventHandler<IReadOnlyList<MediaSessionModel>>? SessionsUpdated;

    Task InitializeAsync();
    Task RefreshAsync();
    IReadOnlyList<MediaSessionModel> CurrentSessions { get; }

    Task<bool> PlayAsync(string sessionId);
    Task<bool> PauseAsync(string sessionId);
    Task<bool> TogglePlayPauseAsync(string sessionId);
    Task<bool> SkipNextAsync(string sessionId);
    Task<bool> SkipPreviousAsync(string sessionId);
}
