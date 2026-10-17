using System.Collections.Generic;
using System.Linq;
using NowPlaying.Models;

namespace NowPlaying.Services;

internal sealed record MediaSessionSnapshot
{
    public required string Id { get; init; }
    public required string SourceAppUserModelId { get; init; }
    public required string AppName { get; init; }
    public System.Windows.Media.ImageSource? AppIcon { get; init; }
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public System.Windows.Media.ImageSource? Thumbnail { get; init; }
    public bool IsClosed { get; init; }
    public bool IsPlaying { get; init; }
    public bool? CanPlay { get; init; }
    public bool? CanPause { get; init; }
    public bool? CanSkipNext { get; init; }
    public bool? CanSkipPrevious { get; init; }
    public System.TimeSpan Position { get; init; }
    public System.TimeSpan StartTime { get; init; }
    public System.TimeSpan EndTime { get; init; }
}

internal static class MediaSessionModelFactory
{
    public static MediaSessionModel? Create(MediaSessionSnapshot snapshot)
    {
        if (snapshot.IsClosed)
        {
            return null;
        }

        string title = snapshot.Title ?? string.Empty;
        string artist = snapshot.Artist ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
        {
            title = snapshot.AppName;
        }

        return new MediaSessionModel
        {
            Id = snapshot.Id,
            SourceAppUserModelId = snapshot.SourceAppUserModelId,
            AppName = snapshot.AppName,
            AppIcon = snapshot.AppIcon,
            Title = title,
            Artist = artist,
            Album = snapshot.Album ?? string.Empty,
            Thumbnail = snapshot.Thumbnail,
            IsPlaying = snapshot.IsPlaying,
            CanPlay = snapshot.CanPlay ?? true,
            CanPause = snapshot.CanPause ?? true,
            CanSkipNext = snapshot.CanSkipNext ?? false,
            CanSkipPrevious = snapshot.CanSkipPrevious ?? false,
            Position = snapshot.Position,
            Duration = snapshot.EndTime - snapshot.StartTime,
            LastUpdated = System.DateTime.UtcNow
        };
    }

    public static List<MediaSessionModel> CreateOrdered(IEnumerable<MediaSessionSnapshot?> snapshots) =>
        snapshots
            .Where(snapshot => snapshot != null)
            .Select(snapshot => Create(snapshot!))
            .Where(model => model != null)
            .Select(model => model!)
            .OrderByDescending(model => model.IsPlaying)
            .ThenBy(model => model.AppName)
            .ToList();
}