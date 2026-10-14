using System;
using System.Windows;
using System.Windows.Controls;
using NowPlaying.Services;

namespace NowPlaying.Views;

public partial class OptionsWindow : Window
{
    private readonly UserSettingsService _settings;

    public OptionsWindow(UserSettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        StartupCheckBox.IsChecked = settings.Current.RunAtStartup;
        ThemeComboBox.SelectedIndex = (int)settings.Current.Theme;
    }

    private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        try
        {
            _settings.SetRunAtStartup(StartupCheckBox.IsChecked == true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not update startup setting", MessageBoxButton.OK, MessageBoxImage.Error);
            StartupCheckBox.IsChecked = _settings.Current.RunAtStartup;
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            _settings.SetTheme((ThemePreference)ThemeComboBox.SelectedIndex);
        }
    }

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}