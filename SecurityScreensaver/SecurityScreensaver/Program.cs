using System;
using System.Windows.Forms;

namespace SecurityScreensaver
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Handle screensaver arguments
            if (args.Length > 0)
            {
                string arg = args[0].ToLower().Trim();
                if (arg.StartsWith("/c")) // Configure mode
                {
                    MessageBox.Show("No configuration options yet.");
                }
                else if (arg.StartsWith("/p")) // Preview mode
                {
                    Application.Run(new ScreensaverForm());
                }
                else if (arg.StartsWith("/s")) // Full-screen mode
                {
                    Application.Run(new ScreensaverForm());
                }
            }
            else // No arguments: default to screensaver mode
            {
                Application.Run(new ScreensaverForm());
            }
        }
    }
}