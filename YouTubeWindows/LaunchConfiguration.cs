using System;
using System.IO;

namespace YouTubeWindows
{
    public sealed class LaunchConfiguration
    {
        public static LaunchConfiguration Default { get; } = new LaunchConfiguration(
            "YouTube",
            "https://www.youtube.com/tv",
            @"Resources\youtube_splash_screen.html",
            true,
            @"Resources\icon.ico");

        public string AppTitle { get; }
        public string StartUrl { get; }
        public bool ShowSplashScreen { get; }
        public string SplashScreenPath { get; }
        public string IconPath { get; }

        public LaunchConfiguration(string appTitle, string startUrl, string splashScreenPath = null, bool showSplashScreen = true, string iconPath = null)
        {
            AppTitle = string.IsNullOrWhiteSpace(appTitle) ? "YouTube" : appTitle;
            StartUrl = string.IsNullOrWhiteSpace(startUrl) ? "https://www.youtube.com/tv" : startUrl;
            SplashScreenPath = splashScreenPath;
            ShowSplashScreen = showSplashScreen;
            IconPath = iconPath;
        }

        public string LoadSplashScreenHtml()
        {
            if (!ShowSplashScreen)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(SplashScreenPath))
            {
                var splashScreenPath = SplashScreenPath;
                if (!Path.IsPathRooted(splashScreenPath))
                {
                    splashScreenPath = Path.Combine(AppContext.BaseDirectory, splashScreenPath);
                }

                if (File.Exists(splashScreenPath))
                {
                    return File.ReadAllText(splashScreenPath);
                }
            }

            return Resource.youtube_splash_screen;
        }
    }
}
