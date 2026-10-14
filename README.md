# NowPlaying for Windows 11

A macOS-inspired "Now Playing" system tray applet for Windows 11 built in modern C# and WPF on .NET 10.

It detects and controls media playing across desktop and browser applications (Spotify, Chrome, Edge, Firefox, Brave, Apple Music, VLC, etc.) via the native Windows System Media Transport Controls (GSMTC) API, displays a Windows 11 Fluent flyout with rich artwork and playback controls, and lets you switch directly to the active media window with a single click.

---

## Features

- **System Tray Integration**: Crisp system tray icon that dynamically highlights when audio/video is playing.
- **macOS-Style Control Center Flyout**: Modern rounded dark-themed acrylic flyout positioned right above the Windows 11 system tray.
- **One-Click Window Activation**: Click the app chip (e.g. `Google Chrome ↗` or `Spotify ↗`) to automatically restore and bring that application's window to the foreground.
- **Rich Media Metadata**: Displays high-resolution album artwork, track title, artist, album name, and playback progress bar.
- **Full Playback Controls**: Interactive Previous, Play/Pause, and Next buttons bound to the active media session.
- **Multi-Player Support**: Automatically tracks multiple concurrent or paused media sessions with currently playing audio sorted to the top.
- **Dismiss on Blur**: Automatically hides when you click outside the flyout, matching native Windows 11 taskbar behavior.
- **Startup Option**: Choose whether the app starts when you sign in to Windows. Startup launches directly to the tray.
- **Theme Options**: Choose Light, Dark, or follow the Windows app-mode setting. Theme changes apply immediately.

---

## Architecture & C# Concepts Demonstrated

This project serves as a practical, modern open-source learning project for C# and Windows desktop development:

1. **WinRT API Interop in .NET 10**:
   - Uses `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager` directly without external wrapper packages by targeting `net10.0-windows10.0.19041.0`.
   - Reads asynchronous thumbnail streams (`IRandomAccessStreamReference`) and converts them into frozen WPF `BitmapSource` objects for thread-safe UI rendering.
2. **Win32 P/Invoke & Window Management**:
   - Uses `EnumWindows`, `GetWindowThreadProcessId`, `ShowWindow`, and `SetForegroundWindow` with `AttachThreadInput` to reliably bring background/minimized windows to the front on Windows 10/11.
3. **MVVM with Source Generators**:
   - Powered by `CommunityToolkit.Mvvm`: uses `[ObservableProperty]` and `[RelayCommand]` attributes to eliminate boilerplate boilerplate.
4. **Dependency Injection**:
   - Uses `Microsoft.Extensions.DependencyInjection` to decouple media management, window activation, view models, and views.
5. **Dynamic System Tray Icon**:
   - Generates anti-aliased 32x32 GDI+ tray icons on the fly with active (playing) and idle (paused) states without requiring external image assets.
6. **User Preferences**:
   - Saves theme selection in `%LocalAppData%\NowPlaying\settings.json` and registers startup through the current user's Windows Run key.

---

## Project Structure

```text
NowPlaying/
├── Models/
│   └── MediaSessionModel.cs        # Immutable record representing session snapshot
├── Services/
│   ├── IWindowFocusService.cs     # Interface for window activation
│   ├── WindowFocusService.cs      # Win32 P/Invoke implementation
│   ├── IMediaManagerService.cs    # Interface for GSMTC media control
│   ├── MediaManagerService.cs     # WinRT media session tracking & thumbnail extraction
│   ├── AppInfoHelper.cs           # AUMID resolution, app names, and executable icons
│   ├── TrayIconHelper.cs          # Dynamic GDI+ tray icon generation
│   └── UserSettingsService.cs     # Theme preference and Windows startup registration
├── ViewModels/
│   ├── MediaCardViewModel.cs      # ViewModel for individual player cards with commands
│   └── NowPlayingFlyoutViewModel.cs# ViewModel managing the active session collection
├── Views/
│   ├── NowPlayingFlyout.xaml      # Windows 11 Fluent dark flyout UI
│   ├── NowPlayingFlyout.xaml.cs   # Tray positioning and blur dismissal logic
│   ├── OptionsWindow.xaml         # Startup and appearance options
│   └── OptionsWindow.xaml.cs      # Applies option changes
├── App.xaml / App.xaml.cs          # Tray-only app lifecycle, DI setup & context menu
└── NowPlaying.csproj              # Project file targeting net10.0-windows10.0.19041.0
```

---

## Requirements & Running the App

- **OS**: Windows 10 (Build 19041+) or Windows 11
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run in Debug Mode:
```powershell
dotnet run
```

### Build Release Executable:
```powershell
dotnet build -c Release
```
The compiled binary will be located in [bin/Release/net10.0-windows10.0.19041.0/NowPlaying.exe](bin/Release/net10.0-windows10.0.19041.0/NowPlaying.exe).

### Run Unit Tests:
```powershell
dotnet test NowPlaying.Tests/NowPlaying.Tests.csproj
```

Right-click the tray icon and choose **Options...** to change the startup and appearance settings. Enabling startup adds a per-user Windows sign-in entry; it does not require administrator privileges.
