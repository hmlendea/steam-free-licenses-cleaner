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

            logger.Info(MyOperation.LicensesCleaning, OperationStatus.Started);

            webProcessor.GoToUrl(LicensesUrl);

            By loginButtonSelector = By.Id("login_btn_signin");

            if (webProcessor.DoesElementExist(loginButtonSelector))
            {
                logger.Error(MyOperation.LicensesCleaning, "The user is not logged in");
                return;
            }

            RemoveLicenses();

            logger.Debug(MyOperation.LicensesCleaning, OperationStatus.Success);

            cookiesManager.SaveCookies();
        }

        void RemoveLicenses()
        {
            int rowIndex = 115;

            while (true)
            {
                string rowXpath = $"//*[@id='main_content']/div/div/div/div/table/tbody/tr[{rowIndex + 2}]";
                By rowSelector = By.XPath($"{rowXpath}/td[2]");
                By removalLinkSelector = By.XPath($"{rowXpath}/td[2]/div/a");

                if (!webProcessor.DoesElementExist(rowSelector))
                {
                    break;
                }
                
                string productName = GetProductName(rowIndex);

                Console.WriteLine(rowIndex + " " + productName);

                if (webProcessor.DoesElementExist(removalLinkSelector) &&
                    patternsToRemove.Any(pattern => Regex.IsMatch(productName, pattern)))
                {
                    string packageId = webProcessor
                        .GetHyperlink(removalLinkSelector)
                        .Split('(')[1]
                        .Split(',')[0];
                    
                    RemoveLicense(packageId);
                    
                    logger.Debug(
                        MyOperation.LicensesCleaning,
                        OperationStatus.InProgress,
                        new LogInfo(MyLogInfoKey.ProductName, productName));
                }
                
                rowIndex += 1;
            }
        }
        
        void RemoveLicense(string packageId)
        {
            string sessionId = GetCookieValue("sessionid");
            string cookiesHeaderValue =
                $"browserid={GetCookieValue("browserid")}; " +
                $"sessionid={sessionId}; " +
                $"timezoneOffset={GetCookieValue("timezoneOffset")}; " +
                //steamMachineAuth
                $"birthtime={GetCookieValue("birthtime")}; " +
                $"lastagecheckage={GetCookieValue("lastagecheckage")}; " +
                $"_ga={GetCookieValue("_ga")}; " +
                $"dp_user_language={GetCookieValue("dp_user_language")}; " +
                $"dp_user_sessionid={GetCookieValue("dp_user_sessionid")}; " +
                $"steamRememberLogin={GetCookieValue("steamRememberLogin")}; " +
                $"beta={GetCookieValue("beta")}; " +
                $"recentapps={GetCookieValue("recentapps")}; " +
                $"steamLoginSecure={GetCookieValue("steamLoginSecure")}; " +
                $"Steam_Language={GetCookieValue("Steam_Language")}; " +
                $"app_impressions={GetCookieValue("app_impressions")}; " +
                $"steamCountry={GetCookieValue("steamCountry")}";

            string content = $"sessionid={sessionId}&packageid={packageId}";
            byte[] contentData = Encoding.ASCII.GetBytes(content);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://store.steampowered.com/account/removelicense");
            request.Method = "POST";
            request.ContentLength = content.Length;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Headers.Add("Cookies", cookiesHeaderValue);

            Console.WriteLine(cookiesHeaderValue);
            Console.WriteLine(sessionId);
            Console.WriteLine(content);

            using (var stream = request.GetRequestStream())
            {
                stream.Write(contentData, 0, content.Length);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            Console.WriteLine(responseString);
        }

        string GetCookieValue(string cookieName)
        {
            string rawValue = webDriver.Manage().Cookies.AllCookies.First(x => x.Name == cookieName).Value;

            return rawValue
                .Replace("%25", "%");
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
