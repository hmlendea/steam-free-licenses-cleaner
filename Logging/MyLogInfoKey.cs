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
    }
}