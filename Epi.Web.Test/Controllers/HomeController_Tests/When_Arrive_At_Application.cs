using System;
using System.Web.Mvc;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.HomeController_Tests
{
    [TestFixture]
    public class When_Arrive_At_Application
    {
        private FakeSurveyFacade _facade;
        private HomeController _controller;
        private FakeHttpContext _httpContext;

        [SetUp]
        public void SetUp()
        {
            _facade = new FakeSurveyFacade { SurveyInfo = SurveyFixtures.SurveyInfo(DateTime.Now.AddDays(30)) };
            _controller = new HomeController(_facade);
            _httpContext = ControllerTestContext.Attach(_controller);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [Test]
        public void Then_See_Survey_Introduction_Page()
        {
            var result = _controller.Index(SurveyFixtures.SurveyId) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("Index"));
            Assert.That(result.Model, Is.SameAs(_facade.SurveyInfo));
            Assert.That(_facade.RequestedSurveyIds, Is.EqualTo(new[] { SurveyFixtures.SurveyId }));
        }

        [Test]
        public void Then_A_New_Response_And_PassCode_Are_Issued()
        {
            var model = (SurveyInfoModel)((ViewResult)_controller.Index(SurveyFixtures.SurveyId)).Model;

            Guid responseId;
            Assert.That(Guid.TryParse(model.ResponseId, out responseId), Is.True);
            Assert.That(_httpContext.Session["RootResponseId"], Is.EqualTo(model.ResponseId));
            Assert.That(_httpContext.Session["RootFormId"], Is.EqualTo(SurveyFixtures.SurveyId));
            Assert.That(_controller.TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID], Is.EqualTo(model.ResponseId));
            Assert.That(model.PassCode, Has.Length.EqualTo(4));
            Assert.That(_controller.TempData["PassCode"], Is.EqualTo(model.PassCode));
        }

        [Test]
        public void Then_Introduction_Text_Is_Prepared_For_Html()
        {
            var model = (SurveyInfoModel)((ViewResult)_controller.Index(SurveyFixtures.SurveyId)).Model;

            Assert.That(model.IntroductionText, Is.EqualTo("Line one<br />Line &nbsp;two"));
        }

        [TestCase(false, "final")]
        [TestCase(true, "draft")]
        public void Then_Draft_Mode_Style_Reflects_The_Survey(bool isDraftMode, string expectedClass)
        {
            _facade.SurveyInfo.IsDraftMode = isDraftMode;

            var model = (SurveyInfoModel)((ViewResult)_controller.Index(SurveyFixtures.SurveyId)).Model;

            Assert.That(model.IsDraftModeStyleClass, Is.EqualTo(expectedClass));
        }

        [Test]
        public void Then_Analytics_Is_Disabled_When_Not_Configured()
        {
            var result = (ViewResult)_controller.Index(SurveyFixtures.SurveyId);

            Assert.That(((Omniture)result.ViewBag.Omniture).IsEnabled, Is.False);
        }

        [Test]
        public void Then_Service_Failure_Shows_Exception_View()
        {
            _facade.SurveyInfo = null;

            var result = _controller.Index(SurveyFixtures.SurveyId) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("Exception"));
        }

        [Test]
        public void Then_Default_Action_Shows_Default_View()
        {
            var result = _controller.Default() as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("Default"));
        }
    }
}
