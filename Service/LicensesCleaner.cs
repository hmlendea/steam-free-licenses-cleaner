using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;

using SteamFreeLicensesCleaner.Logging;

namespace SteamFreeLicensesCleaner.Service
{
    public sealed class LicensesCleaner : ILicensesCleaner
    {
        const string LicensesUrl = "https://store.steampowered.com/account/licenses/";

        readonly IWebProcessor webProcessor;
        readonly ILogger logger;

        public LicensesCleaner(
            IWebProcessor webProcessor,
            ILogger logger)
        {
            this.webProcessor = webProcessor;
            this.logger = logger;
        }

        public void CleanLicenses()
        {
            logger.Info(MyOperation.LicensesCleaning, OperationStatus.Started);

            webProcessor.GoToUrl(LicensesUrl);

            By loginButtonSelector = By.Id("login_btn_signin");

            if (webProcessor.DoesElementExist(loginButtonSelector))
            {
                logger.Error(MyOperation.LicensesCleaning, "The user is not logged in");
                return;
            }

            CleanDemoLicenses();

            logger.Debug(MyOperation.LicensesCleaning, OperationStatus.Success);
        }

        private void CleanDemoLicenses()
        {
            logger.Info(MyOperation.LicensesCleaning, OperationStatus.InProgress, "Cleaning the demo licenses");
        }
    }    
}
