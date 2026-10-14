using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace NowPlaying.Services;

public static class TrayIconHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Generates a crisp Windows 11 system tray icon for Now Playing.
    /// </summary>
    /// <param name="isPlaying">If true, draws an active accent indicator.</param>
    public static Icon CreateNowPlayingIcon(bool isPlaying)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            // Base icon color: vibrant accent if playing, clean white/gray if paused
            Color primaryColor = isPlaying ? Color.FromArgb(0, 164, 255) : Color.FromArgb(235, 235, 235);
            using var brush = new SolidBrush(primaryColor);
            using var pen = new Pen(primaryColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            // Draw a stylish pair of musical notes (beamed eighth notes)
            // Left stem: (10, 8) to (10, 22)
            g.DrawLine(pen, 10, 8, 10, 22);
            // Right stem: (22, 5) to (22, 19)
            g.DrawLine(pen, 22, 5, 22, 19);

            // Beam connecting stems: (10, 8) to (22, 5) with thicker width
            using var beamPen = new Pen(primaryColor, 3.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(beamPen, 10, 8, 22, 5);

            // Left note head (ellipse at bottom of left stem)
            g.FillEllipse(brush, 5, 18, 9, 7);

            // Right note head (ellipse at bottom of right stem)
            g.FillEllipse(brush, 17, 15, 9, 7);

            // If actively playing, draw a subtle green dot at the top right
            if (isPlaying)
            {
                using var dotBrush = new SolidBrush(Color.FromArgb(0, 220, 110));
                g.FillEllipse(dotBrush, 24, 2, 6, 6);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }
}
