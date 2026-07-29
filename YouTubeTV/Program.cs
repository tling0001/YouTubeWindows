using System;
using System.Windows.Forms;

namespace YouTubeWindows
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args, new LaunchConfiguration(
                "YouTube TV",
                "https://www.youtube.com/tv/upg",
                @"Resources\youtubetv_splash_screen.html",
                showSplashScreen: false,
                @"Resources\yt tv.ico")));
        }
    }
}