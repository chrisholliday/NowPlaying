using System.IO;
using NowPlaying.Services;

namespace NowPlaying.Tests;

public sealed class UserSettingsServiceTests
{
    [Fact]
    public void Settings_DefaultAndPersistAcrossServiceInstances()
    {
        string directory = CreateTemporaryDirectory();
        string settingsPath = Path.Combine(directory, "settings.json");
        bool startupRegistered = false;
        var appliedThemes = new List<ThemePreference>();

        try
        {
            using (var settings = CreateService(settingsPath, () => startupRegistered, enabled => startupRegistered = enabled, appliedThemes.Add))
            {
                Assert.Equal(ThemePreference.System, settings.Current.Theme);
                Assert.False(settings.Current.RunAtStartup);

                settings.SetTheme(ThemePreference.Dark);
                settings.SetRunAtStartup(true);
            }

            using var reloaded = CreateService(settingsPath, () => startupRegistered, enabled => startupRegistered = enabled, appliedThemes.Add);

            Assert.Equal(ThemePreference.Dark, reloaded.Current.Theme);
            Assert.True(reloaded.Current.RunAtStartup);
            Assert.Equal([ThemePreference.System, ThemePreference.Dark, ThemePreference.Dark], appliedThemes);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MalformedSettingsFileFallsBackToDefaults()
    {
        string directory = CreateTemporaryDirectory();
        string settingsPath = Path.Combine(directory, "settings.json");
        File.WriteAllText(settingsPath, "not valid json");

        try
        {
            using var settings = CreateService(settingsPath, () => false, _ => { }, _ => { });

            Assert.Equal(ThemePreference.System, settings.Current.Theme);
            Assert.False(settings.Current.RunAtStartup);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static UserSettingsService CreateService(
        string path,
        Func<bool> isRegisteredForStartup,
        Action<bool> setStartupRegistration,
        Action<ThemePreference> applyTheme) =>
        new(path, isRegisteredForStartup, setStartupRegistration, applyTheme);

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"NowPlaying.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}