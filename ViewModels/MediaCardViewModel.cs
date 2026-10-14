using System;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NowPlaying.Models;
using NowPlaying.Services;

namespace NowPlaying.ViewModels;

public partial class MediaCardViewModel : ObservableObject
{
    private readonly IMediaManagerService _mediaService;
    private readonly IWindowFocusService _windowFocusService;

    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _sourceAppUserModelId;

    [ObservableProperty]
    private string _appName;

    [ObservableProperty]
    private ImageSource? _appIcon;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _artist = string.Empty;

    [ObservableProperty]
    private string _album = string.Empty;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _canPlay;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canSkipNext;

    [ObservableProperty]
    private bool _canSkipPrevious;

    [ObservableProperty]
    private TimeSpan _position;

    [ObservableProperty]
    private TimeSpan _duration;

    public bool HasThumbnail => Thumbnail != null;

    public bool HasTimeline => Duration > TimeSpan.Zero;

    public double ProgressPercentage
    {
        get
        {
            if (Duration <= TimeSpan.Zero) return 0;
            double p = (Position.TotalSeconds / Duration.TotalSeconds) * 100.0;
            return Math.Clamp(p, 0, 100);
        }
    }

    public string PositionText => FormatTime(Position);

    public string DurationText => FormatTime(Duration);

    public MediaCardViewModel(
        MediaSessionModel model,
        IMediaManagerService mediaService,
        IWindowFocusService windowFocusService)
    {
        _mediaService = mediaService;
        _windowFocusService = windowFocusService;

        _id = model.Id;
        _sourceAppUserModelId = model.SourceAppUserModelId;
        _appName = model.AppName;
        _appIcon = model.AppIcon;
        UpdateFromModel(model);
    }

    public void UpdateFromModel(MediaSessionModel model)
    {
        Id = model.Id;
        SourceAppUserModelId = model.SourceAppUserModelId;
        AppName = model.AppName;
        AppIcon = model.AppIcon;
        Title = string.IsNullOrWhiteSpace(model.Title) ? model.AppName : model.Title;
        Artist = model.Artist;
        Album = model.Album;
        Thumbnail = model.Thumbnail;
        IsPlaying = model.IsPlaying;
        CanPlay = model.CanPlay;
        CanPause = model.CanPause;
        CanSkipNext = model.CanSkipNext;
        CanSkipPrevious = model.CanSkipPrevious;
        Position = model.Position;
        Duration = model.Duration;

        OnPropertyChanged(nameof(HasThumbnail));
        OnPropertyChanged(nameof(HasTimeline));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(PositionText));
        OnPropertyChanged(nameof(DurationText));
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"m\:ss");
    }

    [RelayCommand]
    private async Task TogglePlayPauseAsync()
    {
        await _mediaService.TogglePlayPauseAsync(Id);
    }

    [RelayCommand]
    private async Task SkipNextAsync()
    {
        await _mediaService.SkipNextAsync(Id);
    }

    [RelayCommand]
    private async Task SkipPreviousAsync()
    {
        await _mediaService.SkipPreviousAsync(Id);
    }

    [RelayCommand]
    private void FocusApp()
    {
        _windowFocusService.FocusApplication(SourceAppUserModelId);
    }
}
