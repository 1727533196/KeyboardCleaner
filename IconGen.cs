using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

class IconGen
{
    static void Main()
    {
        using (var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Background circle: dark blue
            using (var bgBrush = new SolidBrush(Color.FromArgb(0x1A, 0x1A, 0x2E)))
                g.FillEllipse(bgBrush, 0, 0, 31, 31);

            // Accent ring: teal/green
            using (var ringPen = new Pen(Color.FromArgb(0x2E, 0xCC, 0x71), 2))
                g.DrawEllipse(ringPen, 1, 1, 29, 29);

            // Simple lock body (white)
            using (var lockPen = new Pen(Color.White, 3))
            {
                lockPen.StartCap = lockPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                // Shackle (arch)
                g.DrawArc(lockPen, 9, 6, 14, 13, 0, 180);
                // Lock body
                g.DrawRectangle(lockPen, 8, 16, 16, 12);
                // Keyhole
                using (var holeBrush = new SolidBrush(Color.FromArgb(0x1A, 0x1A, 0x2E)))
                    g.FillEllipse(holeBrush, 14, 19, 4, 4);
            }

            // Save as .ico
            IntPtr hIcon = bmp.GetHicon();
            using (var icon = Icon.FromHandle(hIcon))
            {
                using (var fs = new FileStream("app.ico", FileMode.Create))
                    icon.Save(fs);
            }
            DestroyIcon(hIcon);
        }
        Console.WriteLine("app.ico created.");
    }

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr hIcon);
}
