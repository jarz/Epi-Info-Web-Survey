using System;
using System.Web.Mvc;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Test.Fakes;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.SurveyController_Tests
{
    /// <summary>
    /// Entry checks that SurveyController performs before any form rendering or check code runs.
    /// Replaces the old Then_Submit_Survey test, which had no assertions and used a removed POST signature.
    /// </summary>
    [TestFixture]
    public class When_Survey_Cannot_Be_Entered
    {
        private FakeSurveyFacade _facade;
        private SurveyController _controller;

        [SetUp]
        public void SetUp()
        {
            _facade = new FakeSurveyFacade
            {
                SurveyInfo = SurveyFixtures.SurveyInfo(DateTime.Now.AddDays(30)),
                SurveyAnswer = SurveyFixtures.SurveyAnswer()
            };
            _controller = new SurveyController(_facade);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [Test]
        public void Then_PassCode_In_Url_Redirects_To_First_Page()
        {
            ControllerTestContext.Attach(_controller, "http://localhost/Survey/" + SurveyFixtures.ResponseId + "/ab12");

            var result = _controller.Index(SurveyFixtures.ResponseId) as RedirectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Url, Is.EqualTo("/Survey/" + SurveyFixtures.ResponseId + "/1"));
            Assert.That(_facade.RequestedResponseIds, Is.Empty);
        }

        [Test]
        public void Then_Closed_Survey_Shows_Closed_View_On_Get()
        {
            _facade.SurveyInfo.ClosingDate = DateTime.Now.AddDays(-1);
            ControllerTestContext.Attach(_controller, "http://localhost/Survey/" + SurveyFixtures.ResponseId + "/1");

            var result = _controller.Index(SurveyFixtures.ResponseId, 1) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("SurveyClosedError"));
        }

        [Test]
        public void Then_Submitted_Response_Shows_Already_Submitted_View_On_Get()
        {
            _facade.SurveyAnswer.Status = 3;
            ControllerTestContext.Attach(_controller, "http://localhost/Survey/" + SurveyFixtures.ResponseId + "/1", ControllerTestContext.AndroidUserAgent, true);

            var result = _controller.Index(SurveyFixtures.ResponseId, 1) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("IsSubmitedError"));
        }

        [Test]
        public void Then_Closed_Survey_Shows_Closed_View_On_Post()
        {
            _facade.SurveyInfo.ClosingDate = DateTime.Now.AddDays(-1);
            ControllerTestContext.Attach(_controller, "http://localhost/Survey/" + SurveyFixtures.ResponseId + "/1");

            var result = _controller.Index(new SurveyAnswerModel { ResponseId = SurveyFixtures.ResponseId }, "Submit", null, null, null, null, null) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("SurveyClosedError"));
        }

        [Test]
        public void Then_Submitted_Response_Shows_Already_Submitted_View_On_Post()
        {
            _facade.SurveyAnswer.Status = 3;
            ControllerTestContext.Attach(_controller, "http://localhost/Survey/" + SurveyFixtures.ResponseId + "/1");

            var result = _controller.Index(new SurveyAnswerModel { ResponseId = SurveyFixtures.ResponseId }, "Submit", null, null, null, null, null) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("IsSubmitedError"));
            Assert.That(_facade.RequestedResponseIds, Is.EqualTo(new[] { SurveyFixtures.ResponseId }));
        }

        [Test]
        public void Then_Response_Form_State_Is_Read_From_Response_Xml()
        {
            var xml = SurveyFixtures.ResponseXml("textbox1,multiline");

            Assert.That(SurveyController.GetResponseFormState(xml, "RequiredFieldsList"), Is.EqualTo("textbox1,multiline"));
            Assert.That(SurveyController.GetResponseFormState(xml, "HiddenFieldsList"), Is.Empty);
            Assert.That(SurveyController.GetResponseFormState(null, "RequiredFieldsList"), Is.Empty);
        }
    }
}
