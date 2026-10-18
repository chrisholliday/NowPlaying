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

            Color primaryColor = isPlaying ? Color.FromArgb(0, 164, 255) : Color.FromArgb(245, 245, 245);
            Color outlineColor = Color.FromArgb(255, 24, 24, 24);
            using var brush = new SolidBrush(primaryColor);
            using var outlinePen = new Pen(outlineColor, 5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var noteHeadPen = new Pen(outlineColor, 1.25f);
            using var pen = new Pen(primaryColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            // Draw a stylish pair of musical notes (beamed eighth notes)
            // Left stem: (10, 8) to (10, 22)
            g.DrawLine(outlinePen, 10, 8, 10, 22);
            g.DrawLine(pen, 10, 8, 10, 22);
            // Right stem: (22, 5) to (22, 19)
            g.DrawLine(outlinePen, 22, 5, 22, 19);
            g.DrawLine(pen, 22, 5, 22, 19);

            // Beam connecting stems: (10, 8) to (22, 5) with thicker width
            g.DrawLine(outlinePen, 10, 8, 22, 5);
            using var beamPen = new Pen(primaryColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(beamPen, 10, 8, 22, 5);

            // Left note head (ellipse at bottom of left stem)
            g.FillEllipse(brush, 5, 18, 9, 7);
            g.DrawEllipse(noteHeadPen, 5, 18, 9, 7);

            // Right note head (ellipse at bottom of right stem)
            g.FillEllipse(brush, 17, 15, 9, 7);
            g.DrawEllipse(noteHeadPen, 17, 15, 9, 7);

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
