using NuciLog.Core;

namespace SteamFreeLicensesCleaner.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        MyLogInfoKey(string name)
            : base(name)
        {
            
        }

        public static LogInfoKey ProductName => new MyLogInfoKey(nameof(ProductName));

        public static LogInfoKey LincenseIndex => new MyLogInfoKey(nameof(LincenseIndex));

        public static LogInfoKey LicensesCount => new MyLogInfoKey(nameof(LicensesCount));
    }
}