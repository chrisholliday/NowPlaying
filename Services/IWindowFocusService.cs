namespace NowPlaying.Services;

/// <summary>
/// Service for finding and bringing application windows to the foreground.
/// </summary>
public interface IWindowFocusService
{
    /// <summary>
    /// Attempts to bring the main window of the application matching the given AppUserModelId or process name to the foreground.
    /// </summary>
    /// <param name="sourceAppUserModelId">The AppUserModelId or process identifier from GSMTC.</param>
    /// <returns>True if a window was successfully located and focused, otherwise false.</returns>
    bool FocusApplication(string sourceAppUserModelId);
}
