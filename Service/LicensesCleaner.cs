using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using NuciExtensions;
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
        readonly IWebDriver webDriver;
        readonly ILogger logger;

        static IEnumerable<string> patternsToRemove = new List<string>
        {
            "^.* Demo$",
            "^.* Beta Testing$"
        };

        public LicensesCleaner(
            ICookiesManager cookiesManager,
            IWebProcessor webProcessor,
            IWebDriver webDriver,
            ILogger logger)
        {
            this.cookiesManager = cookiesManager;
            this.webProcessor = webProcessor;
            this.webDriver = webDriver;
            this.logger = logger;
        }

        public void CleanLicenses()
        {
            cookiesManager.LoadCookies();

            webProcessor.GoToUrl(LicensesUrl);

            By loginButtonSelector = By.Id("login_btn_signin");

            if (webProcessor.DoesElementExist(loginButtonSelector))
            {
                logger.Error(MyOperation.LicensesCleaning, "The user is not logged in");
                return;
            }
            
            RemoveLicenses();

            cookiesManager.SaveCookies();
        }

        void RemoveLicenses()
        {
            logger.Info(MyOperation.LicensesCleaning, OperationStatus.Started);

            int rowIndex = 0;

            while (true)
            {
                string rowXpath = $"//*[@id='main_content']/div/div/div/div/table/tbody/tr[{rowIndex + 2}]";
                By rowSelector = By.XPath($"{rowXpath}/td[2]");
                By removalLinkSelector = By.XPath($"{rowXpath}/td[2]/div/a");

                if (!webProcessor.DoesElementExist(rowSelector))
                {
                    break;
                }

                try
                {
                    string productName = GetProductName(rowIndex);

                    if (webProcessor.DoesElementExist(removalLinkSelector) &&
                        patternsToRemove.Any(pattern => Regex.IsMatch(productName, pattern)))
                    {
                        string removalScript = webProcessor.GetHyperlink(removalLinkSelector);
                        RemoveLicense(removalScript);

                        logger.Info(
                            MyOperation.LicensesCleaning,
                            OperationStatus.InProgress,
                            new LogInfo(MyLogInfoKey.ProductName, productName),
                            new LogInfo(MyLogInfoKey.LincenseIndex, rowIndex));
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(MyOperation.LicensesCleaning, OperationStatus.InProgress, ex);
                }
                finally
                {
                    rowIndex += 1;
                }
            }

            logger.Debug(MyOperation.LicensesCleaning, OperationStatus.Success);
        }

        void RemoveLicense(string removalScript)
        {
            By licensesTableSelector = By.ClassName("account_table");
            By confirmationButtonSelector = By.XPath($"//*[contains(@class,'btn_green_white_innerfade')]");

            webProcessor.WaitForElementToBeVisible(licensesTableSelector);

            webProcessor.ExecuteScript(removalScript);
            webProcessor.Click(confirmationButtonSelector);
            webProcessor.WaitForElementToBeInvisible(confirmationButtonSelector);

            webProcessor.GoToUrl("https://google.ro");
            webProcessor.GoToUrl(LicensesUrl);
            webProcessor.WaitForElementToBeVisible(licensesTableSelector);
        }

        string GetProductName(int index)
        {
            By rowSelector = By.XPath($"//*[@id='main_content']/div/div/div/div/table/tbody/tr[{index + 2}]/td[2]");
            string productName = webProcessor.GetText(rowSelector).Replace(Environment.NewLine, " ");
            
            return ProcessProductName(productName);
        }

        string ProcessProductName(string productName)
        {
            if (productName.StartsWith("Remove"))
            {
                productName = productName.ReplaceFirst("Remove", "");
            }

            return productName.Trim();
        }
    }    
}
