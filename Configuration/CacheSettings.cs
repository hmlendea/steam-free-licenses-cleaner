using System.IO;

namespace SteamFreeLicensesCleaner.Configuration
{
    public sealed class CacheSettings
    {
        public string CacheDirectoryPath { get; set; }

        public string CookiesFilePath => Path.Combine(CacheDirectoryPath, "cookies.txt");
    }
}
