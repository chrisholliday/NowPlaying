using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NowPlaying.Services;

public partial class WindowFocusService : IWindowFocusService
{
    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BringWindowToTop(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    public bool FocusApplication(string sourceAppUserModelId)
    {
        if (string.IsNullOrWhiteSpace(sourceAppUserModelId))
        {
            return false;
        }

        IntPtr targetHWnd = FindWindowForApp(sourceAppUserModelId);
        if (targetHWnd == IntPtr.Zero)
        {
            return false;
        }

        BringToFront(targetHWnd);
        return true;
    }

    private IntPtr FindWindowForApp(string aumid)
    {
        // 1. Try to extract candidate process name(s)
        var candidateNames = ExtractCandidateProcessNames(aumid);

        var matchingHwnds = new List<(IntPtr Hwnd, int Score)>();

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd))
            {
                return true;
            }

            int textLength = GetWindowTextLength(hWnd);
            if (textLength == 0)
            {
                return true;
            }

            GetWindowThreadProcessId(hWnd, out uint processId);
            if (processId == 0)
            {
                return true;
            }

            try
            {
                using var process = Process.GetProcessById((int)processId);
                string procName = process.ProcessName;

                foreach (var candidate in candidateNames)
                {
                    if (string.Equals(procName, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        var sb = new StringBuilder(textLength + 1);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();

                        // Prefer windows that have a meaningful title
                        int score = string.IsNullOrWhiteSpace(title) ? 1 : 10;
                        matchingHwnds.Add((hWnd, score));
                        break;
                    }
                }
            }
            catch
            {
                // Process may have exited or access denied
            }

            return true;
        }, IntPtr.Zero);

        if (matchingHwnds.Count > 0)
        {
            // Pick highest scored window
            matchingHwnds.Sort((a, b) => b.Score.CompareTo(a.Score));
            return matchingHwnds[0].Hwnd;
        }

        return IntPtr.Zero;
    }

    private static List<string> ExtractCandidateProcessNames(string aumid)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // e.g. "Spotify.exe", "C:\...\chrome.exe"
        if (aumid.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            string fileName = Path.GetFileNameWithoutExtension(aumid);
            candidates.Add(fileName);
        }
        else if (aumid.Contains(".exe", StringComparison.OrdinalIgnoreCase))
        {
            int idx = aumid.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            string sub = aumid[..idx];
            int slash = sub.LastIndexOfAny(new[] { '\\', '/' });
            string name = slash >= 0 ? sub[(slash + 1)..] : sub;
            candidates.Add(name);
        }

        // Handle UWP / Store AUMID e.g. "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify"
        if (aumid.Contains('!'))
        {
            var parts = aumid.Split('!');
            if (parts.Length > 1)
            {
                candidates.Add(parts[1]);
            }
            if (parts.Length > 0 && parts[0].Contains('.'))
            {
                var pkgParts = parts[0].Split('.');
                foreach (var p in pkgParts)
                {
                    if (p.Length > 3)
                    {
                        candidates.Add(p);
                    }
                }
            }
        }

        // Common known media players mapping
        if (aumid.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("Spotify");
        }
        if (aumid.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("chrome");
        }
        if (aumid.Contains("Edge", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("msedge");
        }
        if (aumid.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("firefox");
        }
        if (aumid.Contains("Brave", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("brave");
        }
        if (aumid.Contains("AppleMusic", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("AppleMusic");
        }
        if (aumid.Contains("VLC", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add("vlc");
        }

        // Also add the raw aumid as a candidate
        candidates.Add(aumid);

        return [.. candidates];
    }

    private static void BringToFront(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }
        else
        {
            ShowWindow(hWnd, SW_SHOW);
        }

        IntPtr foregroundWnd = GetForegroundWindow();
        uint foregroundThreadId = GetWindowThreadProcessId(foregroundWnd, out _);
        uint currentThreadId = GetCurrentThreadId();

        if (foregroundThreadId != currentThreadId)
        {
            AttachThreadInput(currentThreadId, foregroundThreadId, true);
            SetForegroundWindow(hWnd);
            BringWindowToTop(hWnd);
            AttachThreadInput(currentThreadId, foregroundThreadId, false);
        }
        else
        {
            SetForegroundWindow(hWnd);
            BringWindowToTop(hWnd);
        }
    }
}
