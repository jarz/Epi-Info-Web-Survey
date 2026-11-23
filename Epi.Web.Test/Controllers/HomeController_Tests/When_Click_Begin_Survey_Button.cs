using System.Linq;
using Epi.Web.MVC.Test.Fakes;
using Epi.Web.MVC.Utility;
using MvcDynamicForms.Fields;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Controllers.HomeController_Tests
{
    /// <summary>
    /// Begin Survey (HomeController POST Index) creates a response through the survey service, so the
    /// controller action itself is not exercised here. This covers the page it opens: the facade builds the
    /// first page with FormProvider.GetForm. Previously this test called the removed SurveyController.Notify action.
    /// </summary>
    [TestFixture]
    public class When_Click_Begin_Survey_Button
    {
        [Test]
        public void Then_Open_The_Survey()
        {
            var form = FormProvider.GetForm(SurveyFixtures.SurveyInfoDto(), 1, SurveyFixtures.SurveyAnswer());

            Assert.That(form.Fields, Has.Count.EqualTo(7));
            Assert.That(form.Fields.Values.First(), Is.TypeOf<TextBox>());
            Assert.That(form.Fields["textbox1"], Is.TypeOf<TextBox>());
            Assert.That(form.CurrentPage, Is.EqualTo(1));
            Assert.That(form.NumberOfPages, Is.EqualTo(1));
        }
    }
}
