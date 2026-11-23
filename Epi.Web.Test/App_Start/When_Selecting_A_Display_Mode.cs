using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using Epi.Web.MVC.Test.Fakes;
using Epi.Web.MVC.Test.Views;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.App_Start
{
    /// <summary>
    /// Characterizes the display modes Application_Start registers through <see cref="DisplayModeConfig"/>, evaluated by the
    /// real Web Pages <see cref="DisplayModeProvider"/> against fake requests. View lookup is checked only at the
    /// display-mode level (which file the provider picks among the views on disk); Razor compilation and the hosted
    /// view engine are not exercised.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class When_Selecting_A_Display_Mode
    {
        private const string IPad = "iPad";
        private const string OperaMobi = "Opera Mobi";
        private const string Android = "Android";
        private const string BrowserCapabilitiesMobile = "MVC built-in Mobile (browser capabilities / override)";
        private const string Default = "Default (desktop)";

        // Evaluation order after registration on the stock provider: the last Insert(0, ...) is evaluated first.
        private static readonly string[] RegisteredOrder = { IPad, OperaMobi, Android, BrowserCapabilitiesMobile, Default };

        private const string IPadLegacyUserAgent = "Mozilla/5.0 (iPad; CPU OS 12_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1.2 Mobile/15E148 Safari/604.1";
        private const string IPadOsDesktopUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15";
        private const string OperaMobileOnAndroidUserAgent = "Opera/9.80 (Android 2.3.3; Linux; Opera Mobi/ADR-1111101157; U; en) Presto/2.9.201 Version/11.50";
        private const string OperaMobileOnSymbianUserAgent = "Opera/9.80 (S60; SymbOS; Opera Mobi/499; U; en-GB) Presto/2.4.18 Version/10.00";
        private const string AndroidTabletUserAgent = "Mozilla/5.0 (Linux; Android 13; SM-X700) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36";
        private const string IPhoneUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
        private const string DesktopUserAgent = ControllerTestContext.DesktopUserAgent;
        private const string AndroidPhoneUserAgent = ControllerTestContext.AndroidUserAgent;
        private const string OverrideCookie = ".ASPXBrowserOverride";

        private List<IDisplayMode> _stockModes;

        private static IList<IDisplayMode> Modes { get { return DisplayModeProvider.Instance.Modes; } }

        [SetUp]
        public void RegisterOnStockProvider()
        {
            _stockModes = Modes.ToList();
            Assert.That(_stockModes.Select(mode => mode.DisplayModeId), Is.EqualTo(new[] { "Mobile", "" }), "Expected the stock MVC display modes before registration.");

            DisplayModeConfig.RegisterDisplayModes(Modes);
        }

        [TearDown]
        public void RestoreStockProvider()
        {
            Modes.Clear();
            foreach (IDisplayMode mode in _stockModes)
            {
                Modes.Add(mode);
            }
        }

        private static string SelectedMode(HttpContextBase context)
        {
            IDisplayMode selected = DisplayModeProvider.Instance.GetAvailableDisplayModesForContext(context, null).First();
            return RegisteredOrder[Modes.IndexOf(selected)];
        }

        private static FakeHttpContext Request(string userAgent, bool browserCapabilitiesSayMobile = false)
        {
            return new FakeHttpContext("http://localhost/", userAgent, browserCapabilitiesSayMobile);
        }

        /// <summary>Sets an override through the real Web Pages API, then replays its cookie on the next request.</summary>
        private static FakeHttpContext NextRequestAfterOverride(string userAgent, bool browserCapabilitiesSayMobile, BrowserOverride browserOverride)
        {
            FakeHttpContext switching = Request(userAgent, browserCapabilitiesSayMobile);
            switching.SetOverriddenBrowser(browserOverride);
            HttpCookie written = switching.Response.Cookies[OverrideCookie];
            Assert.That(written, Is.Not.Null);

            FakeHttpContext next = Request(userAgent, browserCapabilitiesSayMobile);
            next.Request.Cookies.Add(new HttpCookie(written.Name, written.Value));
            return next;
        }

        [Test]
        public void Then_Three_User_Agent_Modes_Precede_The_Stock_Modes_And_All_Map_To_Mobile()
        {
            Assert.That(Modes.Count, Is.EqualTo(RegisteredOrder.Length));
            Assert.That(Modes.Select(mode => mode.DisplayModeId), Is.EqualTo(new[] { "Mobile", "Mobile", "Mobile", "Mobile", "" }));
            Assert.That(Modes.Skip(3), Is.EqualTo(_stockModes));
        }

        [TestCase(IPadLegacyUserAgent, false, ExpectedResult = IPad)]
        [TestCase(IPadOsDesktopUserAgent, false, ExpectedResult = Default, Description = "iPadOS 13+ Safari sends a macOS user agent")]
        [TestCase(OperaMobileOnSymbianUserAgent, false, ExpectedResult = OperaMobi)]
        [TestCase(OperaMobileOnAndroidUserAgent, false, ExpectedResult = OperaMobi, Description = "Opera Mobi is evaluated before Android")]
        [TestCase(AndroidPhoneUserAgent, false, ExpectedResult = Android)]
        [TestCase(AndroidPhoneUserAgent, true, ExpectedResult = Android, Description = "User-agent modes win over browser capabilities")]
        [TestCase(AndroidTabletUserAgent, false, ExpectedResult = Android, Description = "Tablets get the phone layout")]
        [TestCase("mozilla/5.0 (linux; android 14)", false, ExpectedResult = Android, Description = "Matching is case-insensitive")]
        [TestCase(IPhoneUserAgent, true, ExpectedResult = BrowserCapabilitiesMobile)]
        [TestCase(IPhoneUserAgent, false, ExpectedResult = Default, Description = "iPhone is mobile only when browser capabilities say so")]
        [TestCase(DesktopUserAgent, false, ExpectedResult = Default)]
        [TestCase("", false, ExpectedResult = Default)]
        public string Then_The_First_Matching_Mode_Is_Selected(string userAgent, bool browserCapabilitiesSayMobile)
        {
            return SelectedMode(Request(userAgent, browserCapabilitiesSayMobile));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Then_A_Request_Without_A_User_Agent_Throws(bool browserCapabilitiesSayMobile)
        {
            // Existing defect: the registered conditions dereference Request.UserAgent without a null check, and the
            // iPad condition is evaluated first, so every display-mode lookup for such a request throws.
            var thrown = Assert.Throws<NullReferenceException>(() => SelectedMode(Request(null, browserCapabilitiesSayMobile)));
            Assert.That(thrown.StackTrace, Does.Contain(typeof(DisplayModeConfig).FullName));
        }

        [TestCase(AndroidPhoneUserAgent, ExpectedResult = Android)]
        [TestCase(IPadLegacyUserAgent, ExpectedResult = IPad)]
        [TestCase(OperaMobileOnSymbianUserAgent, ExpectedResult = OperaMobi)]
        public string Then_A_Desktop_Override_Does_Not_Affect_User_Agent_Modes(string userAgent)
        {
            FakeHttpContext overridden = NextRequestAfterOverride(userAgent, true, BrowserOverride.Desktop);
            Assert.That(overridden.GetOverriddenUserAgent(), Is.Not.EqualTo(userAgent), "The desktop override cookie should be in effect.");

            // Existing defect: the custom conditions read the raw User-Agent, so "switch to desktop" cannot take effect.
            // Only the three custom conditions are evaluated: the built-in mode builds browser capabilities for the
            // overridden user agent through SimpleWorkerRequest, which needs a hosted ASP.NET AppDomain.
            IDisplayMode first = Modes.Take(3).First(mode => mode.CanHandleContext(overridden));
            return RegisteredOrder[Modes.IndexOf(first)];
        }

        [TestCase(AndroidPhoneUserAgent, "~/Views/Home/Index.cshtml", ExpectedResult = "~/Views/Home/Index.Mobile.cshtml")]
        [TestCase(DesktopUserAgent, "~/Views/Home/Index.cshtml", ExpectedResult = "~/Views/Home/Index.cshtml")]
        [TestCase(IPadLegacyUserAgent, "~/Views/Survey/Index.cshtml", ExpectedResult = "~/Views/Survey/Index.Mobile.cshtml")]
        [TestCase(AndroidPhoneUserAgent, "~/Views/Shared/_Layout.cshtml", ExpectedResult = "~/Views/Shared/_Layout.Mobile.cshtml")]
        [TestCase(DesktopUserAgent, "~/Views/Shared/_Layout.cshtml", ExpectedResult = "~/Views/Shared/_Layout.cshtml")]
        [TestCase(AndroidPhoneUserAgent, "~/Views/Shared/_LayoutPage2.cshtml", ExpectedResult = "~/Views/Shared/_LayoutPage2.cshtml", Description = "No mobile variant: falls through to the default mode")]
        [TestCase(AndroidPhoneUserAgent, "~/Views/FormResponse/Index.cshtml", ExpectedResult = "~/Views/FormResponse/Index.Mobile.cshtml")]
        [TestCase(DesktopUserAgent, "~/Views/FormResponse/Index.cshtml", ExpectedResult = null, Description = "Existing defect: FormResponse has only a mobile view")]
        public string Then_View_Files_Resolve_Against_The_Views_On_Disk(string userAgent, string virtualPath)
        {
            DisplayInfo resolved = DisplayModeProvider.Instance.GetDisplayInfoForVirtualPath(virtualPath, Request(userAgent), WebProject.FileExists, null);
            return resolved == null ? null : resolved.FilePath;
        }

        [Test]
        public void Then_Application_Start_Registers_Display_Modes_Only_Through_DisplayModeConfig()
        {
            // Static wiring check: Application_Start needs IIS hosting, WCF and Unity, so it is not invoked here.
            var activeDisplayModeLines = File.ReadAllLines(Path.Combine(WebProject.Root, "Global.asax.cs"))
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith("//", StringComparison.Ordinal) && line.Contains("DisplayMode"));

            Assert.That(activeDisplayModeLines, Is.EqualTo(new[] { "DisplayModeConfig.RegisterDisplayModes(DisplayModeProvider.Instance.Modes);" }));
        }
    }
}
