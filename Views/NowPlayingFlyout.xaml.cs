using System;
using System.Windows;
using NowPlaying.ViewModels;

namespace NowPlaying.Views;

public partial class NowPlayingFlyout : Window
{
    private readonly NowPlayingFlyoutViewModel _viewModel;

    public NowPlayingFlyout(NowPlayingFlyoutViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            ShowFlyout();
        }
    }

    public void ShowFlyout()
    {
        _ = _viewModel.RefreshAsync();
        Show();
        UpdateLayout();
        PositionNearTray();
        Activate();
        Focus();
    }

    private void PositionNearTray()
    {
        // Compute position near Windows 11 notification area (system tray)
        var workArea = SystemParameters.WorkArea;

        double targetWidth = ActualWidth > 0 ? ActualWidth : (Width > 0 ? Width : 380);
        double targetHeight = ActualHeight > 0 ? ActualHeight : 320;

        // Position at bottom-right corner with 16px margin from work area boundaries
        Left = Math.Max(0, workArea.Right - targetWidth - 16);
        Top = Math.Max(0, workArea.Bottom - targetHeight - 16);
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
