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
            @"^.* - Free$",
            @"^.* (Free-To-Play|Free To Play|Free 2 Play|F2P)$",
            @"^.* (PEGI|ESRB|USK|BBFC|CTC|Unrated).*$",
            @"^.* (Test|Dedicated) Server .*$",
            @"^.* (Trial|Demo|Free|Free Play) (Edition|Version|Ver[\.]+).*$",
            @"^.* \((Trial|Demo|Demo Free|Demo Old|Demo \(Old\)|Demo [0-9].*)\)$",
            @"^.* \(RETIRED FREE PACKAGE\)$",
            @"^.* 30-Day Trial$",
            @"^.* Beta Testing$",
            @"^.* Character Creator Preview$",
            @"^.* Free to Play$",
            @"^.* Free Trial$",
            @"^.* Short Film.*$",
            @"^.* System Test$",
            @"^.*[ _](Demo|Demo [0-9].*)$",
            @"^.*[ _](Trailer|Teaser).*$",
            @"^.*Tech Video.*$",
            @"^.*Video Commentary.*$",
            @"^Blade Tutorial: 3Ds Max.*$",
            @"^Complete Figure Drawing Course.*$",

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
            
            LoadLicenses();
            RemoveLicenses();

            cookiesManager.SaveCookies();
        }

        void LoadLicenses()
        {
            By rowsSelector = By.XPath(@"//*[@id='main_content']/div/div/div/div/table/tbody/tr");
            By loginButtonSelector = By.Id("login_btn_signin");

            logger.Info(MyOperation.LicensesLoading, OperationStatus.Started);

            webProcessor.GoToUrl(LicensesUrl);

            if (webProcessor.DoesElementExist(loginButtonSelector))
            {
                logger.Error(MyOperation.LicensesLoading, "The user is not logged in");
                return;
            }

            int rowsCount = webProcessor.GetElements(rowsSelector).Count();

            while(webProcessor.GetElements(rowsSelector).Count() != rowsCount)
            {
                webProcessor.Wait(1500);
                rowsCount = webProcessor.GetElements(rowsSelector).Count();
            }

            logger.Info(
                MyOperation.LicensesLoading,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.LicensesCount, rowsCount));
        }

        void RemoveLicenses()
        {
            string rowXpath = $"//*[@id='main_content']/div/div/div/div/table/tbody/tr";

            By rowsSelector = By.XPath(rowXpath);

            int rowsCount = webProcessor.GetElements(rowsSelector).Count() - 2;

            logger.Info(
                MyOperation.LicensesCleaning,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.LicensesCount, rowsCount));

            for (int rowIndex = 0; rowIndex < rowsCount; rowIndex++)
            {
                string rowIndexXpath = $"{rowXpath}[{rowIndex + 2}]";

                By productNameSelector = By.XPath($"{rowIndexXpath}/td[2]");
                By removalLinkSelector = By.XPath($"{rowIndexXpath}/td[2]/div/a");

                webProcessor.WaitForElementToExist(productNameSelector);
                IWebElement element = webDriver.FindElement(productNameSelector);

                Actions actions = new Actions(webDriver);
                actions.MoveToElement(element);
                actions.Perform();

                try
                {
                    string productName = ProcessProductName(webProcessor.GetText(productNameSelector));

                    if (webProcessor.DoesElementExist(removalLinkSelector) &&
                        patternsToRemove.Any(pattern => Regex.IsMatch(productName, pattern, RegexOptions.IgnoreCase)))
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

            webProcessor.Refresh();
            webProcessor.WaitForElementToBeInvisible(confirmationButtonSelector);
            webProcessor.WaitForElementToBeInvisible(licensesTableSelector);
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
