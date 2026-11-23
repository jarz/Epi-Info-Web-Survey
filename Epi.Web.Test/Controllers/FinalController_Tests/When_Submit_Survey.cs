using System;
using System.Collections;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.FinalController_Tests
{
    [TestFixture]
    public class When_Submit_Survey
    {
        private FakeSurveyFacade _facade;
        private FinalController _controller;

        [SetUp]
        public void SetUp()
        {
            // FinalController.Index calls FormsAuthentication.SignOut(), which reads HttpContext.Current and its
            // browser capabilities; outside IIS those capabilities must be supplied.
            var request = new HttpRequest(string.Empty, "http://localhost/", string.Empty);
            request.Browser = new HttpBrowserCapabilities { Capabilities = new Hashtable { { "cookies", "true" } } };
            HttpContext.Current = new HttpContext(request, new HttpResponse(TextWriter.Null));
            _facade = new FakeSurveyFacade { SurveyInfo = SurveyFixtures.SurveyInfo(DateTime.Now.AddDays(30)) };
            _controller = new FinalController(_facade);
            ControllerTestContext.Attach(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            HttpContext.Current = null;
        }

        [Test]
        public void Then_Show_Finalized_Page()
        {
            var result = _controller.Index(SurveyFixtures.SurveyId, "final", SurveyFixtures.ResponseId) as ViewResult;

            // The finalized page used to be the "PostSubmit" view; the current action renders Final/Index.
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("Index"));
            Assert.That(result.Model, Is.TypeOf<SurveyInfoModel>());
            Assert.That(result.ViewBag.ResponseId, Is.EqualTo(SurveyFixtures.ResponseId));
        }

        [Test]
        public void Then_Exit_Text_Is_Prepared_For_Html()
        {
            var model = (SurveyInfoModel)((ViewResult)_controller.Index(SurveyFixtures.SurveyId, "final", SurveyFixtures.ResponseId)).Model;

            Assert.That(model.ExitText, Is.EqualTo("Thank you<br />for &nbsp;taking part"));
            Assert.That(model.PassCode, Has.Length.EqualTo(4));
            Assert.That(model.IsDraftModeStyleClass, Is.EqualTo("final"));
        }

        [Test]
        public void Then_Service_Failure_Shows_Exception_View()
        {
            _facade.SurveyInfo = null;

            var result = _controller.Index(SurveyFixtures.SurveyId, "final", SurveyFixtures.ResponseId) as ViewResult;

            Assert.That(result.ViewName, Is.EqualTo("Exception"));
        }
    }
}
