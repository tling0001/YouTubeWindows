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
                "YouTube Kids",
                "https://www.youtube.com/tv/kids",
                showSplashScreen: false)));
        }
    }
}
