using System;
using System.Collections.Generic;
using System.IO;

namespace NowPlaying.Services;

internal static class WindowProcessMatcher
{
    public static List<string> ExtractCandidateProcessNames(string aumid)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddTokenizedCandidates(string value)
        {
            foreach (string token in value.Split(['!', '.', '_', '-', ' ', '\\', '/', ':', ','], StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length >= 3)
                {
                    candidates.Add(token);
                }
            }
        }

        if (aumid.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(Path.GetFileNameWithoutExtension(aumid));
        }
        else if (aumid.Contains(".exe", StringComparison.OrdinalIgnoreCase))
        {
            int idx = aumid.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            string sub = aumid[..idx];
            int slash = sub.LastIndexOfAny(['\\', '/']);
            candidates.Add(slash >= 0 ? sub[(slash + 1)..] : sub);
        }

        if (aumid.Contains('!'))
        {
            string[] parts = aumid.Split('!');
            if (parts.Length > 1)
            {
                candidates.Add(parts[1]);
                AddTokenizedCandidates(parts[1]);
            }

            if (parts.Length > 0)
            {
                AddTokenizedCandidates(parts[0]);
            }
        }

        AddTokenizedCandidates(aumid);

        if (aumid.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) candidates.Add("Spotify");
        if (aumid.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("chrome");
            candidates.Add("chromium");
        }
        if (aumid.Contains("Edge", StringComparison.OrdinalIgnoreCase)) candidates.Add("msedge");
        if (aumid.Contains("Firefox", StringComparison.OrdinalIgnoreCase)) candidates.Add("firefox");
        if (aumid.Contains("Brave", StringComparison.OrdinalIgnoreCase)) candidates.Add("brave");
        if (aumid.Contains("AppleMusic", StringComparison.OrdinalIgnoreCase)) candidates.Add("AppleMusic");
        if (aumid.Contains("VLC", StringComparison.OrdinalIgnoreCase)) candidates.Add("vlc");

        candidates.Add(aumid);
        return [.. candidates];
    }

    public static int GetMatchScore(string processName, IReadOnlyCollection<string> candidates, bool hasWindowTitle)
    {
        int bestScore = -1;
        foreach (string candidate in candidates)
        {
            bool exactMatch = string.Equals(processName, candidate, StringComparison.OrdinalIgnoreCase);
            bool matches = exactMatch
                || processName.Contains(candidate, StringComparison.OrdinalIgnoreCase)
                || candidate.Contains(processName, StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                continue;
            }

            int score = (exactMatch ? 20 : 12) + (hasWindowTitle ? 4 : -4);
            bestScore = Math.Max(bestScore, score);
        }

        return bestScore;
    }
}