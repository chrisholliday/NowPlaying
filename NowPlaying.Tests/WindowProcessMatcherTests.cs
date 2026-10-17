using NowPlaying.Services;

namespace NowPlaying.Tests;

public sealed class WindowProcessMatcherTests
{
    [Theory]
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!App", "Spotify")]
    [InlineData("Google.Chrome!App", "chrome")]
    [InlineData("Google.Chrome!App", "chromium")]
    [InlineData("Microsoft.MicrosoftEdge!App", "msedge")]
    [InlineData("VideoLAN.VLC!App", "vlc")]
    public void ExtractCandidateProcessNames_IncludesKnownExecutableAliases(string aumid, string expected)
    {
        var candidates = WindowProcessMatcher.ExtractCandidateProcessNames(aumid);

        Assert.Contains(candidates, candidate => string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetMatchScore_PrefersExactProcessAndTitledWindows()
    {
        var candidates = new[] { "chrome", "chromium" };

        int exactWithTitle = WindowProcessMatcher.GetMatchScore("chrome", candidates, hasWindowTitle: true);
        int fuzzyWithTitle = WindowProcessMatcher.GetMatchScore("chromium-browser", candidates, hasWindowTitle: true);
        int exactWithoutTitle = WindowProcessMatcher.GetMatchScore("chrome", candidates, hasWindowTitle: false);

        Assert.True(exactWithTitle > fuzzyWithTitle);
        Assert.True(exactWithTitle > exactWithoutTitle);
        Assert.Equal(-1, WindowProcessMatcher.GetMatchScore("notepad", candidates, hasWindowTitle: true));
    }
}