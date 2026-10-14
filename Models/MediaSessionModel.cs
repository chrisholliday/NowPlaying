using System;
using System.Windows.Media;

namespace NowPlaying.Models;

public record MediaSessionModel
{
    public required string Id { get; init; }
    public required string SourceAppUserModelId { get; init; }
    public required string AppName { get; init; }
    public ImageSource? AppIcon { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public ImageSource? Thumbnail { get; init; }
    public bool IsPlaying { get; init; }
    public bool CanPlay { get; init; }
    public bool CanPause { get; init; }
    public bool CanSkipNext { get; init; }
    public bool CanSkipPrevious { get; init; }
    public TimeSpan Position { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}
