using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NowPlaying.Services;

public static class AppInfoHelper
{
    private static readonly ConcurrentDictionary<string, string> NameCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Lazy<ImageSource?>> IconCache = new(StringComparer.OrdinalIgnoreCase);

    public static string GetFriendlyAppName(string aumid)
    {
        if (string.IsNullOrWhiteSpace(aumid))
        {
            return "Media Player";
        }

        if (NameCache.TryGetValue(aumid, out var cachedName))
        {
            return cachedName;
        }

        string resolved = ResolveName(aumid);
        NameCache[aumid] = resolved;
        return resolved;
    }

    public static ImageSource? GetAppIcon(string aumid)
    {
        if (string.IsNullOrWhiteSpace(aumid))
        {
            return null;
        }

        var cachedIcon = IconCache.GetOrAdd(aumid, static key => new Lazy<ImageSource?>(() =>
        {
            var icon = ResolveIcon(key);
            if (icon != null && icon.CanFreeze)
            {
                icon.Freeze();
            }

            return icon;
        }));

        return cachedIcon.Value;
    }

    private static string ResolveName(string aumid)
    {
        // Known mapping
        if (aumid.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) return "Spotify";
        if (aumid.Contains("chrome", StringComparison.OrdinalIgnoreCase)) return "Google Chrome";
        if (aumid.Contains("msedge", StringComparison.OrdinalIgnoreCase) || aumid.Contains("MicrosoftEdge", StringComparison.OrdinalIgnoreCase)) return "Microsoft Edge";
        if (aumid.Contains("firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox";
        if (aumid.Contains("brave", StringComparison.OrdinalIgnoreCase)) return "Brave";
        if (aumid.Contains("AppleMusic", StringComparison.OrdinalIgnoreCase)) return "Apple Music";
        if (aumid.Contains("vlc", StringComparison.OrdinalIgnoreCase)) return "VLC Media Player";
        if (aumid.Contains("ZuneMusic", StringComparison.OrdinalIgnoreCase) || aumid.Contains("Media.Player", StringComparison.OrdinalIgnoreCase)) return "Media Player";
        if (aumid.Contains("foobar2000", StringComparison.OrdinalIgnoreCase)) return "foobar2000";
        if (aumid.Contains("TIDAL", StringComparison.OrdinalIgnoreCase)) return "TIDAL";

        // If it's a file path or exe name
        if (aumid.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                string fileName = Path.GetFileName(aumid);
                // Try finding running process
                string procName = Path.GetFileNameWithoutExtension(fileName);
                var procs = Process.GetProcessesByName(procName);
                if (procs.Length > 0)
                {
                    string desc = procs[0].MainModule?.FileVersionInfo.FileDescription ?? "";
                    if (!string.IsNullOrWhiteSpace(desc)) return desc;
                }
                return procName;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(aumid);
            }
        }

        if (aumid.Contains('!'))
        {
            var parts = aumid.Split('!');
            return parts[^1];
        }

        return aumid;
    }

    private static ImageSource? ResolveIcon(string aumid)
    {
        try
        {
            string? exePath = FindExecutablePath(aumid);
            if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
            {
                using var sysIcon = Icon.ExtractAssociatedIcon(exePath);
                if (sysIcon != null)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        sysIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
        }
        catch
        {
            // Ignore icon extraction failures
        }

        return null;
    }

    private static string? FindExecutablePath(string aumid)
    {
        if (File.Exists(aumid))
        {
            return aumid;
        }

        string cleanName = aumid;
        if (cleanName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            cleanName = Path.GetFileNameWithoutExtension(cleanName);
        }
        else if (cleanName.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) cleanName = "Spotify";
        else if (cleanName.Contains("chrome", StringComparison.OrdinalIgnoreCase)) cleanName = "chrome";
        else if (cleanName.Contains("msedge", StringComparison.OrdinalIgnoreCase)) cleanName = "msedge";
        else if (cleanName.Contains("firefox", StringComparison.OrdinalIgnoreCase)) cleanName = "firefox";
        else if (cleanName.Contains("brave", StringComparison.OrdinalIgnoreCase)) cleanName = "brave";
        else if (cleanName.Contains("vlc", StringComparison.OrdinalIgnoreCase)) cleanName = "vlc";

        try
        {
            var procs = Process.GetProcessesByName(cleanName);
            if (procs.Length > 0 && procs[0].MainModule != null)
            {
                return procs[0].MainModule?.FileName;
            }
        }
        catch
        {
            // Restricted access
        }

        return null;
    }
}
