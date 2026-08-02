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
                "YouTube Music",
                "https://www.youtube.com/tv#/browse?c=FEtopics_music",
                @"Resources\music_splash_screen.html",
                showSplashScreen: true,
                @"Resources\yt music.ico")));
        }
    }
}
