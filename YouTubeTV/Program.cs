using System;
using System.Windows.Forms;

namespace YouTubeWindows
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args, new LaunchConfiguration(
                "YouTube TV",
                "https://www.youtube.com/tv/upg",
                @"C:\Users\tling\Downloads\youtubetv_splash_screen.html")));
        }
    }
}