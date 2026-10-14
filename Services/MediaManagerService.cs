using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using NowPlaying.Models;

namespace NowPlaying.Services;

public class MediaManagerService : IMediaManagerService
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private readonly ConcurrentDictionary<string, GlobalSystemMediaTransportControlsSession> _activeSessions = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private List<MediaSessionModel> _currentSessions = new();
    private System.Threading.Timer? _pollTimer;

    public event EventHandler<IReadOnlyList<MediaSessionModel>>? SessionsUpdated;

    public IReadOnlyList<MediaSessionModel> CurrentSessions
    {
        get
        {
            lock (_currentSessions)
            {
                return _currentSessions.ToList();
            }
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.SessionsChanged += OnSessionsChanged;
                await RefreshSessionsAsync();

                // Periodic poll (every 2.5 seconds) to catch browser media changes that don't trigger events
                _pollTimer = new System.Threading.Timer(async _ =>
                {
                    await RefreshSessionsAsync();
                }, null, TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(2.5));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize GSMTC: {ex.Message}");
        }
    }

    public Task RefreshAsync()
    {
        return RefreshSessionsAsync();
    }

    private async void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        await RefreshSessionsAsync();
    }

    private async Task RefreshSessionsAsync()
    {
        if (_sessionManager == null) return;

        // Try to acquire lock, if already refreshing, skip
        if (!await _refreshLock.WaitAsync(100))
        {
            return;
        }

        try
        {
            var rawSessions = _sessionManager.GetSessions();
            var newSessionMap = new ConcurrentDictionary<string, GlobalSystemMediaTransportControlsSession>();

            // Process all detected media sessions concurrently with timeout protection
            var extractionTasks = rawSessions.Select(async (session, index) =>
            {
                string aumid = session.SourceAppUserModelId ?? "UnknownApp";
                string sessionId = $"{aumid}_{index}";

                newSessionMap[sessionId] = session;
                SubscribeSessionEvents(session);

                return await ExtractSessionModelAsync(sessionId, session);
            }).ToList();

            var extractedResults = await Task.WhenAll(extractionTasks);

            _activeSessions.Clear();
            foreach (var kvp in newSessionMap)
            {
                _activeSessions[kvp.Key] = kvp.Value;
            }

            var modelList = extractedResults
                .Where(m => m != null)
                .Select(m => m!)
                // Currently playing sessions at top, then paused sessions
                .OrderByDescending(s => s.IsPlaying)
                .ThenBy(s => s.AppName)
                .ToList();

            lock (_currentSessions)
            {
                _currentSessions = modelList;
            }

            SessionsUpdated?.Invoke(this, modelList);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing sessions: {ex.Message}");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private readonly HashSet<GlobalSystemMediaTransportControlsSession> _subscribedSessions = new();

    private void SubscribeSessionEvents(GlobalSystemMediaTransportControlsSession session)
    {
        lock (_subscribedSessions)
        {
            if (_subscribedSessions.Add(session))
            {
                try
                {
                    session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                    session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                    session.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;
                }
                catch
                {
                    // Ignore event subscription issues for closed sessions
                }
            }
        }
    }

    private async void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        await RefreshSessionsAsync();
    }

    private async void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        await RefreshSessionsAsync();
    }

    private async void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await RefreshSessionsAsync();
    }

    private static async Task<MediaSessionModel?> ExtractSessionModelAsync(string sessionId, GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            string aumid = session.SourceAppUserModelId ?? "UnknownApp";
            string appName = AppInfoHelper.GetFriendlyAppName(aumid);
            var appIcon = AppInfoHelper.GetAppIcon(aumid);

            // 1. Playback Info
            GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo = null;
            try
            {
                playbackInfo = session.GetPlaybackInfo();
            }
            catch { }

            // If session is closed, ignore it
            if (playbackInfo != null && playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed)
            {
                return null;
            }

            bool isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            var controls = playbackInfo?.Controls;

            // 2. Media Properties with timeout (1.5 seconds) so unresponsive apps don't block
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps = null;
            try
            {
                var propsTask = session.TryGetMediaPropertiesAsync().AsTask();
                if (await Task.WhenAny(propsTask, Task.Delay(1500)) == propsTask)
                {
                    mediaProps = await propsTask;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving media properties for {aumid}: {ex.Message}");
            }

            // 3. Timeline Properties
            GlobalSystemMediaTransportControlsSessionTimelineProperties? timeline = null;
            try
            {
                timeline = session.GetTimelineProperties();
            }
            catch { }

            // 4. Thumbnail Image
            ImageSource? thumbnail = null;
            if (mediaProps?.Thumbnail != null)
            {
                thumbnail = await LoadThumbnailAsync(mediaProps.Thumbnail);
            }

            string title = mediaProps?.Title ?? string.Empty;
            string artist = mediaProps?.Artist ?? string.Empty;
            string album = mediaProps?.AlbumTitle ?? string.Empty;

            // Fallback title to application name if metadata is empty
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
            {
                title = appName;
            }

            return new MediaSessionModel
            {
                Id = sessionId,
                SourceAppUserModelId = aumid,
                AppName = appName,
                AppIcon = appIcon,
                Title = title,
                Artist = artist,
                Album = album,
                Thumbnail = thumbnail,
                IsPlaying = isPlaying,
                CanPlay = controls?.IsPlayEnabled ?? true,
                CanPause = controls?.IsPauseEnabled ?? true,
                CanSkipNext = controls?.IsNextEnabled ?? false,
                CanSkipPrevious = controls?.IsPreviousEnabled ?? false,
                Position = timeline?.Position ?? TimeSpan.Zero,
                Duration = (timeline?.EndTime ?? TimeSpan.Zero) - (timeline?.StartTime ?? TimeSpan.Zero),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to extract session model for {sessionId}: {ex.Message}");
            return null;
        }
    }

    private static async Task<ImageSource?> LoadThumbnailAsync(IRandomAccessStreamReference streamRef)
    {
        try
        {
            using var cts = new CancellationTokenSource(1500);
            var openTask = streamRef.OpenReadAsync().AsTask(cts.Token);
            using var readStream = await openTask;
            using var netStream = readStream.AsStreamForRead();
            using var memoryStream = new MemoryStream();
            await netStream.CopyToAsync(memoryStream, cts.Token);
            memoryStream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> PlayAsync(string sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            try
            {
                return await session.TryPlayAsync();
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> PauseAsync(string sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            try
            {
                return await session.TryPauseAsync();
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> TogglePlayPauseAsync(string sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            try
            {
                return await session.TryTogglePlayPauseAsync();
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> SkipNextAsync(string sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            try
            {
                return await session.TrySkipNextAsync();
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> SkipPreviousAsync(string sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            try
            {
                return await session.TrySkipPreviousAsync();
            }
            catch { }
        }
        return false;
    }
}
