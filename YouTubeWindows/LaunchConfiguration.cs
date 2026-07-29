using System.IO;

namespace YouTubeWindows
{
    public sealed class LaunchConfiguration
    {
        public static LaunchConfiguration Default { get; } = new LaunchConfiguration(
            "YouTube",
            "https://www.youtube.com/tv");

        public string AppTitle { get; }
        public string StartUrl { get; }
        public string SplashScreenPath { get; }

        public LaunchConfiguration(string appTitle, string startUrl, string splashScreenPath = null)
        {
            AppTitle = string.IsNullOrWhiteSpace(appTitle) ? "YouTube" : appTitle;
            StartUrl = string.IsNullOrWhiteSpace(startUrl) ? "https://www.youtube.com/tv" : startUrl;
            SplashScreenPath = splashScreenPath;
        }

        public string LoadSplashScreenHtml()
        {
            if (!string.IsNullOrWhiteSpace(SplashScreenPath) && File.Exists(SplashScreenPath))
            {
                return File.ReadAllText(SplashScreenPath);
            }

            return Resource.youtube_splash_screen;
        }
    }
}
