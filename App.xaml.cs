using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using NowPlaying.Services;
using NowPlaying.ViewModels;
using NowPlaying.Views;

namespace NowPlaying;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string AppMutexName = @"Local\NowPlaying_SingleInstance_Mutex";
    private Mutex? _instanceMutex;

    private IServiceProvider? _serviceProvider;
    private TaskbarIcon? _taskbarIcon;
    private NowPlayingFlyout? _flyout;
    private Icon? _currentIcon;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(true, AppMutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "Now Playing is already running!\nLook for the music note icon in your system tray (bottom-right taskbar, or inside the ^ overflow menu).",
                "Now Playing",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        _flyout = _serviceProvider.GetRequiredService<NowPlayingFlyout>();
        var mediaService = _serviceProvider.GetRequiredService<IMediaManagerService>();

        SetupTrayIcon(mediaService);

        // Start media monitoring
        await mediaService.InitializeAsync();

        // Show the flyout immediately on launch for instant feedback
        _flyout.ShowFlyout();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IWindowFocusService, WindowFocusService>();
        services.AddSingleton<IMediaManagerService, MediaManagerService>();
        services.AddSingleton<NowPlayingFlyoutViewModel>();
        services.AddSingleton<NowPlayingFlyout>();
    }

    private void SetupTrayIcon(IMediaManagerService mediaService)
    {
        _currentIcon = TrayIconHelper.CreateNowPlayingIcon(false);

        var contextMenu = new ContextMenu();

        var openItem = new MenuItem { Header = "Show Now Playing" };
        openItem.Click += (s, e) => _flyout?.Toggle();

        var exitItem = new MenuItem { Header = "Exit Now Playing" };
        exitItem.Click += (s, e) => Shutdown();

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Now Playing",
            Icon = _currentIcon,
            ContextMenu = contextMenu
        };

        // Force creation in Windows Shell notification area
        _taskbarIcon.ForceCreate();

        _taskbarIcon.TrayLeftMouseDown += (s, e) => _flyout?.Toggle();

        // Dynamically update tray icon and tooltip based on playback
        mediaService.SessionsUpdated += (s, sessions) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_taskbarIcon == null) return;

                var playingSession = sessions.FirstOrDefault(x => x.IsPlaying);
                bool isPlaying = playingSession != null;

                _currentIcon?.Dispose();
                _currentIcon = TrayIconHelper.CreateNowPlayingIcon(isPlaying);
                _taskbarIcon.Icon = _currentIcon;

                if (playingSession != null)
                {
                    string info = string.IsNullOrWhiteSpace(playingSession.Artist)
                        ? $"{playingSession.Title} ({playingSession.AppName})"
                        : $"{playingSession.Title} - {playingSession.Artist}";

                    if (info.Length > 63)
                    {
                        info = info[..60] + "...";
                    }

                    _taskbarIcon.ToolTipText = info;
                }
                else
                {
                    _taskbarIcon.ToolTipText = "Now Playing";
                }
            });
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _taskbarIcon?.Dispose();
        _currentIcon?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();

        base.OnExit(e);
    }
}

