using System;
using System.Web;
using System.Web.Mvc;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.ViewSwitcherController_Tests
{
    /// <summary>
    /// The mobile/desktop switch relies on ASP.NET Web Pages browser overrides (".ASPXBrowserOverride" cookie),
    /// which the jQuery Mobile layouts depend on.
    /// </summary>
    [TestFixture]
    public class When_Switching_View
    {
        private const string OverrideCookie = ".ASPXBrowserOverride";
        private const string ReturnUrl = "/Home/Index/abc";

        [Test]
        public void Then_Desktop_Browser_Can_Request_Mobile_View()
        {
            using (var controller = new ViewSwitcherController())
            {
                var context = ControllerTestContext.Attach(controller);

                RedirectResult result = controller.SwitchView(true, ReturnUrl);

                Assert.That(result.Url, Is.EqualTo(ReturnUrl));
                HttpCookie cookie = context.Response.Cookies[OverrideCookie];
                Assert.That(cookie, Is.Not.Null);
                Assert.That(cookie.Value, Does.Contain("Mobile"));
                Assert.That(cookie.Expires, Is.GreaterThan(DateTime.Now));
            }
        }

        [Test]
        public void Then_Mobile_Browser_Can_Request_Desktop_View()
        {
            using (var controller = new ViewSwitcherController())
            {
                var context = ControllerTestContext.Attach(controller, userAgent: ControllerTestContext.AndroidUserAgent, isMobileDevice: true);

                RedirectResult result = controller.SwitchView(false, ReturnUrl);

                Assert.That(result.Url, Is.EqualTo(ReturnUrl));
                HttpCookie cookie = context.Response.Cookies[OverrideCookie];
                Assert.That(cookie, Is.Not.Null);
                Assert.That(cookie.Value, Is.Not.Empty);
                Assert.That(cookie.Value, Does.Not.Contain("Mobile"));
                Assert.That(cookie.Expires, Is.GreaterThan(DateTime.Now));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Then_Requesting_The_Native_View_Clears_The_Override(bool isMobileDevice)
        {
            using (var controller = new ViewSwitcherController())
            {
                var context = ControllerTestContext.Attach(controller, isMobileDevice: isMobileDevice);

                RedirectResult result = controller.SwitchView(isMobileDevice, ReturnUrl);

                Assert.That(result.Url, Is.EqualTo(ReturnUrl));
                HttpCookie cookie = context.Response.Cookies[OverrideCookie];
                Assert.That(cookie, Is.Not.Null);
                Assert.That(cookie.Expires, Is.LessThan(DateTime.Now));
            }
        }
    }
}
