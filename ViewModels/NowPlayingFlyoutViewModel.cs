using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NowPlaying.Models;
using NowPlaying.Services;

namespace NowPlaying.ViewModels;

public partial class NowPlayingFlyoutViewModel : ObservableObject
{
    private readonly IMediaManagerService _mediaService;
    private readonly IWindowFocusService _windowFocusService;
    private readonly DispatcherTimer _timelineTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    [ObservableProperty]
    private ObservableCollection<MediaCardViewModel> _mediaCards = new();

    [ObservableProperty]
    private MediaCardViewModel? _primarySession;

    [ObservableProperty]
    private bool _hasActiveMedia;

    [ObservableProperty]
    private bool _isAnyPlaying;

    [ObservableProperty]
    private string _headerTitle = "Now Playing";

    public NowPlayingFlyoutViewModel(
        IMediaManagerService mediaService,
        IWindowFocusService windowFocusService)
    {
        _mediaService = mediaService;
        _windowFocusService = windowFocusService;

        _timelineTimer.Tick += (_, _) =>
        {
            DateTime utcNow = DateTime.UtcNow;
            foreach (var card in MediaCards)
            {
                card.UpdateTimelineDisplay(utcNow);
            }
        };

        _mediaService.SessionsUpdated += OnSessionsUpdated;
        UpdateSessions(_mediaService.CurrentSessions);
    }

    public async Task RefreshAsync()
    {
        await _mediaService.RefreshAsync();
    }

    private void OnSessionsUpdated(object? sender, IReadOnlyList<MediaSessionModel> sessions)
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => UpdateSessions(sessions));
        }
        else
        {
            UpdateSessions(sessions);
        }
    }

    public void UpdateSessions(IReadOnlyList<MediaSessionModel> sessions)
    {
        var existingDict = new Dictionary<string, MediaCardViewModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in MediaCards)
        {
            existingDict[card.Id] = card;
        }

        var newCardList = new List<MediaCardViewModel>();
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var session in sessions)
        {
            activeIds.Add(session.Id);
            if (existingDict.TryGetValue(session.Id, out var existingCard))
            {
                existingCard.UpdateFromModel(session);
                newCardList.Add(existingCard);
            }
            else
            {
                var newCard = new MediaCardViewModel(session, _mediaService, _windowFocusService);
                newCardList.Add(newCard);
            }
        }

        // Reconcile MediaCards collection
        // 1. Remove cards that no longer exist
        for (int i = MediaCards.Count - 1; i >= 0; i--)
        {
            if (!activeIds.Contains(MediaCards[i].Id))
            {
                MediaCards.RemoveAt(i);
            }
        }

        // 2. Add or reorder cards to match newCardList
        for (int i = 0; i < newCardList.Count; i++)
        {
            var targetCard = newCardList[i];
            int currentIndex = MediaCards.IndexOf(targetCard);
            if (currentIndex == -1)
            {
                MediaCards.Insert(i, targetCard);
            }
            else if (currentIndex != i)
            {
                MediaCards.Move(currentIndex, i);
            }
        }

        HasActiveMedia = MediaCards.Count > 0;
        PrimarySession = MediaCards.FirstOrDefault(c => c.IsPlaying) ?? MediaCards.FirstOrDefault();
        IsAnyPlaying = MediaCards.Any(c => c.IsPlaying);
        if (IsAnyPlaying)
        {
            _timelineTimer.Start();
        }
        else
        {
            _timelineTimer.Stop();
        }

        HeaderTitle = MediaCards.Count > 1
            ? $"Now Playing ({MediaCards.Count})"
            : "Now Playing";
    }

    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current?.Shutdown();
    }
}
