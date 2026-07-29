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
                "YouTube Kids",
                "https://www.youtube.com/tv/kids",
                @"C:\Users\tling\Downloads\ytkids_splash_screen.html")));
        }
    }
}
