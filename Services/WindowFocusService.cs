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
        var candidateNames = WindowProcessMatcher.ExtractCandidateProcessNames(aumid);
        var matchingHwnds = new List<(IntPtr Hwnd, int Score)>();

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd))
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

                int score = WindowProcessMatcher.GetMatchScore(
                    procName,
                    candidateNames,
                    hasWindowTitle: !string.IsNullOrWhiteSpace(GetWindowTitle(hWnd)));
                if (score >= 0)
                {
                    matchingHwnds.Add((hWnd, score));
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
            matchingHwnds.Sort((a, b) => b.Score.CompareTo(a.Score));
            return matchingHwnds[0].Hwnd;
        }

        return IntPtr.Zero;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        int textLength = GetWindowTextLength(hWnd);
        if (textLength <= 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(textLength + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
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
