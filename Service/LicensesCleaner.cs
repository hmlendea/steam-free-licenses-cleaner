using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NuciExtensions;
using NuciLog.Core;
using NuciWeb;

using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

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
            @"^.* \(Trial\)$",
            @"^.* 30-[Dd]ay [Tt]rial$",
            @"^.* Beta Testing$",
            @"^.* Demo$",
            @"^.* Free Trial$",
            @"^.* System Test$",
            @"^.* Test Server - Free$",
            @"^.* Test Server$",
            @"^.* Trial [Ee]dition$",
            @"^.* Trial [Vv]er\.$",
            @"^.* Trial [Vv]ersion$",
            @"^.* Trial [Vv]ersionⅡ$",

            // Chinese, Japanese, Korean
            @"\p{IsHangulJamo}|"+
            @"\p{IsCJKRadicalsSupplement}|"+
            @"\p{IsCJKSymbolsandPunctuation}|"+
            @"\p{IsEnclosedCJKLettersandMonths}|"+
            @"\p{IsCJKCompatibility}|"+
            @"\p{IsCJKUnifiedIdeographsExtensionA}|"+
            @"\p{IsCJKUnifiedIdeographs}|"+
            @"\p{IsHangulSyllables}|"+
            @"\p{IsCJKCompatibilityForms}"
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

            string rowXpath = $"//*[@id='main_content']/div/div/div/div/table/tbody/tr";

            By rowsSelector = By.XPath(rowXpath);

            int rowsCount = webProcessor.GetElements(rowsSelector).Count();

            for (int rowIndex = 2000; rowIndex < rowsCount; rowIndex++)
            {
                string rowIndexXpath = $"{rowXpath}[{rowIndex + 2}]";

                By productNameSelector = By.XPath($"{rowIndexXpath}/td[2]");
                By removalLinkSelector = By.XPath($"{rowIndexXpath}/td[2]/div/a");

                IWebElement element = webDriver.FindElement(productNameSelector);

                Actions actions = new Actions(webDriver);
                actions.MoveToElement(element);
                actions.Perform();

                try
                {
                    string productName = ProcessProductName(webProcessor.GetText(productNameSelector));
                    Console.WriteLine(rowIndex + " " + productName);

                    if (webProcessor.DoesElementExist(removalLinkSelector) &&
                        patternsToRemove.Any(pattern => Regex.IsMatch(productName, pattern)))
                    {
                        string removalScript = webProcessor.GetHyperlink(removalLinkSelector);
                        RemoveLicense(removalScript);

                        rowIndex -= 1;

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

        string ProcessProductName(string productName)
        {
            string processedProductName = productName.Replace(Environment.NewLine, " ");

            if (processedProductName.StartsWith("Remove"))
            {
                processedProductName = processedProductName.ReplaceFirst("Remove", "");
            }

            return processedProductName.Trim();
        }
    }    
}
