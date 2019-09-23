using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;

using SteamFreeLicensesCleaner.Logging;

namespace SteamFreeLicensesCleaner.Service
{
    public sealed class LicensesCleaner : ILicensesCleaner
    {
        const string LicensesUrl = "https://store.steampowered.com/account/licenses/";

        readonly ICookiesManager cookiesManager;
        readonly IWebProcessor webProcessor;
        readonly ILogger logger;

        public LicensesCleaner(
            ICookiesManager cookiesManager,
            IWebProcessor webProcessor,
            ILogger logger)
        {
            this.cookiesManager = cookiesManager;
            this.webProcessor = webProcessor;
            this.logger = logger;
        }

        public void CleanLicenses()
        {
            cookiesManager.LoadCookies();

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

            cookiesManager.SaveCookies();
        }

        private void CleanDemoLicenses()
        {
            logger.Info(MyOperation.LicensesCleaning, OperationStatus.InProgress, "Cleaning the demo licenses");

            int rowIndex = 0;
            
            while (true)
            {
                string rowXpath = $"//*[@id='main_content']/div/div/div/div/table/tbody/tr[{rowIndex + 2}]";
                By rowSelector = By.XPath($"{rowXpath}/td[2]");

                if (!webProcessor.DoesElementExist(rowSelector))
                {
                    break;
                }

                string productName = webProcessor.GetText(rowSelector);

                rowIndex += 1; 
            }
        }
    }    
}
