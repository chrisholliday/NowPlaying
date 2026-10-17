using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace NowPlaying.Services;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public sealed class UserSettings
{
    public bool RunAtStartup { get; set; }
    public ThemePreference Theme { get; set; } = ThemePreference.System;
}

public sealed class UserSettingsService : IDisposable
{
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "NowPlaying";
    private readonly string _settingsPath;
    private readonly Func<bool> _isRegisteredForStartup;
    private readonly Action<bool> _setStartupRegistration;
    private readonly Action<ThemePreference> _applyTheme;
    private readonly bool _listeningForSystemPreferences;

    public UserSettings Current { get; }

    public UserSettingsService()
        : this(GetDefaultSettingsPath(), IsRegisteredForStartup, SetStartupRegistration, ApplyTheme, listenForSystemPreferences: true)
    {
    }

    internal UserSettingsService(
        string settingsPath,
        Func<bool> isRegisteredForStartup,
        Action<bool> setStartupRegistration,
        Action<ThemePreference> applyTheme,
        bool listenForSystemPreferences = false)
    {
        _settingsPath = settingsPath;
        _isRegisteredForStartup = isRegisteredForStartup;
        _setStartupRegistration = setStartupRegistration;
        _applyTheme = applyTheme;
        _listeningForSystemPreferences = listenForSystemPreferences;
        Current = LoadSettings();
        Current.RunAtStartup = _isRegisteredForStartup();

        if (_listeningForSystemPreferences)
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        _applyTheme(Current.Theme);
    }

    public void SetRunAtStartup(bool enabled)
    {
        _setStartupRegistration(enabled);
        Current.RunAtStartup = enabled;
        SaveSettings();
    }

    public void SetTheme(ThemePreference preference)
    {
        Current.Theme = preference;
        SaveSettings();
        _applyTheme(preference);
    }

    private UserSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_settingsPath))
                    ?? new UserSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not read user settings: {ex.Message}");
        }

        return new UserSettings();
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Current, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not save user settings: {ex.Message}");
            throw;
        }
    }

    private static bool IsRegisteredForStartup()
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath);
        return runKey?.GetValue(RunValueName) is string;
    }

    private static string GetDefaultSettingsPath()
    {
        string settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NowPlaying");
        return Path.Combine(settingsDirectory, "settings.json");
    }

    private static void SetStartupRegistration(bool enabled)
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true);

        if (runKey is null)
        {
            throw new InvalidOperationException("Could not open the current-user startup registry key.");
        }

        if (enabled)
        {
            runKey.SetValue(RunValueName, GetStartupCommand(), RegistryValueKind.String);
        }
        else
        {
            runKey.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    private static string GetStartupCommand()
    {
        string processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine the application path.");

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            string assemblyPath = Assembly.GetEntryAssembly()?.Location
                ?? throw new InvalidOperationException("Could not determine the application assembly path.");
            return $"\"{processPath}\" \"{assemblyPath}\" --startup";
        }

        return $"\"{processPath}\" --startup";
    }

    private void OnUserPreferenceChanged(object? sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        if (Current.Theme == ThemePreference.System && e.Category is Microsoft.Win32.UserPreferenceCategory.Color or Microsoft.Win32.UserPreferenceCategory.General)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => _applyTheme(Current.Theme));
        }
    }

    private static void ApplyTheme(ThemePreference preference)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            ApplicationTheme theme = preference switch
            {
                ThemePreference.Light => ApplicationTheme.Light,
                ThemePreference.Dark => ApplicationTheme.Dark,
                _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark
                    ? ApplicationTheme.Dark
                    : ApplicationTheme.Light
            };

            ApplicationThemeManager.Apply(theme);
            ApplyFlyoutPalette(theme);
        });
    }

    private static void ApplyFlyoutPalette(ApplicationTheme theme)
    {
        var resources = System.Windows.Application.Current.Resources;
        bool dark = theme == ApplicationTheme.Dark;

        SetBrush(resources, "FlyoutBackgroundBrush", dark ? "#EB202022" : "#F5F4F2");
        SetBrush(resources, "FlyoutBorderBrush", dark ? "#30FFFFFF" : "#30000000");
        SetBrush(resources, "HeaderBackgroundBrush", dark ? "#20000000" : "#14FFFFFF");
        SetBrush(resources, "HeaderBorderBrush", dark ? "#15FFFFFF" : "#20000000");
        SetBrush(resources, "CardBackgroundBrush", dark ? "#302A2D34" : "#FFFFFFFF");
        SetBrush(resources, "CardBorderBrush", dark ? "#25FFFFFF" : "#20000000");
        SetBrush(resources, "SurfaceBrush", dark ? "#30FFFFFF" : "#15000000");
        SetBrush(resources, "SurfaceHoverBrush", dark ? "#45FFFFFF" : "#25000000");
        SetBrush(resources, "SurfacePressedBrush", dark ? "#60FFFFFF" : "#35000000");
        SetBrush(resources, "PrimaryTextBrush", dark ? "#FFFFFFFF" : "#FF202020");
        SetBrush(resources, "SecondaryTextBrush", dark ? "#FFB0B5BA" : "#FF505050");
        SetBrush(resources, "MutedTextBrush", dark ? "#FF757D86" : "#FF707070");
        SetBrush(resources, "SubtleTextBrush", dark ? "#80FFFFFF" : "#99000000");
        SetBrush(resources, "CloseTextBrush", dark ? "#A0FFFFFF" : "#99000000");
        SetBrush(resources, "PlaceholderBrush", dark ? "#40FFFFFF" : "#30000000");
        SetBrush(resources, "AccentBrush", "#FF0078D4");
        SetBrush(resources, "AccentHoverBrush", "#FF1086E0");
        SetBrush(resources, "AccentPressedBrush", "#FF006CBE");
        SetBrush(resources, "PlayingBrush", "#FF00D26A");
        SetBrush(resources, "SettingsWindowBackgroundBrush", dark ? "#FF202022" : "#FFF3F3F3");
        SetBrush(resources, "SettingsCardBrush", dark ? "#FF2A2D34" : "#FFFAFAFA");
        SetBrush(resources, "SettingsTextBrush", dark ? "#FFFFFFFF" : "#FF202020");
        SetBrush(resources, "SettingsSecondaryTextBrush", dark ? "#FFB0B5BA" : "#FF606060");
    }

    private static void SetBrush(System.Windows.ResourceDictionary resources, string key, string color)
    {
        resources[key] = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    public void Dispose()
    {
        if (_listeningForSystemPreferences)
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }
    }
}