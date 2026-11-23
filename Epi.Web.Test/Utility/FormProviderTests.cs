using System;
using System.Linq;
using Epi.Web.MVC.Test.Fakes;
using Epi.Web.MVC.Utility;
using MvcDynamicForms;
using MvcDynamicForms.Fields;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Utility
{
    /// <summary>
    /// Server-rendered survey markup that the browser assets (jQuery Mobile, jQuery validationEngine) depend on.
    /// </summary>
    [TestFixture]
    public class FormProviderTests
    {
        private static Form BuildForm(bool isMobile, string requiredFieldsList = "", params Tuple<string, string>[] answers)
        {
            return FormProvider.GetForm(SurveyFixtures.SurveyInfoDto(), 1, SurveyFixtures.SurveyAnswer(1, requiredFieldsList, answers), isMobile);
        }

        [Test]
        public void Desktop_Form_Uses_Desktop_Field_Types()
        {
            Form form = BuildForm(isMobile: false);

            Assert.That(form.IsMobile, Is.False);
            Assert.That(form.Fields["textbox1"], Is.TypeOf<TextBox>());
            Assert.That(form.Fields["labelone"], Is.TypeOf<Literal>());
            Assert.That(form.Fields["multiline"], Is.TypeOf<TextArea>());
            Assert.That(form.Fields["numerictextbox"], Is.TypeOf<NumericTextBox>());
        }

        [Test]
        public void Mobile_Form_Uses_Mobile_Field_Types_And_Wrapper()
        {
            Form form = BuildForm(isMobile: true);

            Assert.That(form.IsMobile, Is.True);
            Assert.That(form.FormWrapperClass, Is.EqualTo("MvcDynamicMobileForm"));
            Assert.That(form.Fields, Has.Count.EqualTo(7));
            Assert.That(form.Fields["textbox1"], Is.TypeOf<MobileTextBox>());
            Assert.That(form.Fields["labelone"], Is.TypeOf<MobileLiteral>());
            Assert.That(form.Fields["multiline"], Is.TypeOf<MobileTextArea>());
            Assert.That(form.Fields["numerictextbox"], Is.TypeOf<MobileNumericTextBox>());
        }

        [Test]
        public void Saved_Responses_Populate_Field_Values()
        {
            Form form = BuildForm(false, "", Tuple.Create("TextBox2", "synthetic answer"));

            Assert.That(((InputField)form.Fields["textbox2"]).Response, Is.EqualTo("synthetic answer"));
        }

        [Test]
        public void Response_Form_State_Is_Copied_To_The_Form()
        {
            Form form = BuildForm(false, "textbox1");

            Assert.That(form.RequiredFieldsList, Is.EqualTo("textbox1"));
            Assert.That(form.HiddenFieldsList, Is.Empty);
        }

        [Test]
        public void Validate_Flags_Empty_Required_Fields_Only()
        {
            Form form = BuildForm(false, "", Tuple.Create("TextBox2", "synthetic answer"));

            bool isValid = form.Validate("textbox1,textbox2");

            var textBox1 = (InputField)form.Fields["textbox1"];
            var textBox2 = (InputField)form.Fields["textbox2"];
            var multiline = (InputField)form.Fields["multiline"];
            Assert.That(isValid, Is.False);
            Assert.That(textBox1.Required, Is.True);
            Assert.That(textBox1.Error, Is.EqualTo(textBox1.RequiredMessage));
            Assert.That(textBox2.Required, Is.True);
            Assert.That(textBox2.Error, Is.Null.Or.Empty);
            Assert.That(multiline.Required, Is.False);
        }

        [Test]
        public void Validate_Passes_When_Nothing_Is_Required()
        {
            Form form = BuildForm(false);

            Assert.That(form.Validate(""), Is.True);
            Assert.That(form.InputFields.All(f => !f.Required), Is.True);
        }

        [Test]
        public void Desktop_Html_Uses_Fixed_Size_Wrapper_And_ValidationEngine_Classes()
        {
            Form form = BuildForm(false);
            form.Validate("textbox1");

            string html = form.RenderHtml();

            Assert.That(form.FormWrapperClass, Is.Not.EqualTo("MvcDynamicMobileForm"));
            Assert.That(html, Does.Contain("class=\"" + form.FormWrapperClass + "\""));
            Assert.That(html, Does.Contain("width:" + form.Width + "px;"));
            Assert.That(html, Does.Not.Contain("data-role=\"fieldcontain\""));
            Assert.That(html, Does.Contain("validate[required]"));
        }

        [Test]
        public void Mobile_Html_Uses_jQuery_Mobile_Attributes()
        {
            Form form = BuildForm(true);

            string html = form.RenderHtml();

            Assert.That(html, Does.Contain("class=\"MvcDynamicMobileForm\""));
            Assert.That(html, Does.Contain("data-role=\"fieldcontain\""));
            Assert.That(html, Does.Contain("data-ajax=\"false\""));
            Assert.That(html, Does.Contain("width:auto;height:auto;"));
        }
    }
}
