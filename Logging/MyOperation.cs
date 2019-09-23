using NuciLog.Core;

namespace SteamFreeLicensesCleaner.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name)
            : base(name)
        {
            
        }

        public static Operation CookiesLoading => new MyOperation(nameof(CookiesLoading));

        public static Operation CookiesSaving => new MyOperation(nameof(CookiesSaving));

        public static Operation LicensesCleaning => new MyOperation(nameof(LicensesCleaning));
    }
}