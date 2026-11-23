using System;
using System.Xml.Linq;
using Epi.Web.MVC.Test.Fakes;
using Epi.Web.MVC.Utility;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Utility
{
    [TestFixture]
    public class SurveyHelperTests
    {
        // Server-side fallback used by SurveyController when Request.Browser.IsMobileDevice is false.
        [TestCase("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Mobile Safari/537.36")]
        [TestCase("Mozilla/5.0 (Linux; Android 13; SM-X700) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36")]
        [TestCase("Opera/9.80 (Android; Opera Mini/7.5.33361/31.1448; U; en) Presto/2.8.119 Version/11.1010")]
        [TestCase("Opera/9.80 (S60; SymbOS; Opera Mobi/499; U; en) Presto/2.4.18 Version/10.00")]
        [TestCase("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1")]
        [TestCase("Mozilla/5.0 (iPad; CPU OS 12_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1 Mobile/15E148 Safari/604.1")]
        [TestCase("Mozilla/5.0 (Windows Phone 10.0; Android 6.0.1; Microsoft; Lumia 950) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0 Mobile Safari/537.36 Edge/15.15063")]
        public void IsMobileDevice_Recognizes_Mobile_User_Agents(string userAgent)
        {
            Assert.That(SurveyHelper.IsMobileDevice(userAgent), Is.True);
        }

        [TestCase("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36")]
        [TestCase("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0")]
        // iPadOS 13+ Safari sends a desktop macOS user agent, so it is treated as a desktop browser.
        [TestCase("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15")]
        public void IsMobileDevice_Treats_Desktop_User_Agents_As_Desktop(string userAgent)
        {
            Assert.That(SurveyHelper.IsMobileDevice(userAgent), Is.False);
        }

        [Test]
        public void GetPassCode_Returns_Four_Hex_Characters()
        {
            string passCode = SurveyHelper.GetPassCode();

            Assert.That(passCode, Does.Match("^[0-9a-f]{4}$"));
        }

        [TestCase("2e1d01d4-f50d-4f23-888b-cd4b7fc9884b", true)]
        [TestCase("{2E1D01D4-F50D-4F23-888B-CD4B7FC9884B}", true)]
        [TestCase("2e1d01d4f50d4f23888bcd4b7fc9884b", false)]
        [TestCase("ab12", false)]
        [TestCase(null, false)]
        public void IsGuid_Accepts_Only_Hyphenated_Guids(string value, bool expected)
        {
            Assert.That(SurveyHelper.IsGuid(value), Is.EqualTo(expected));
        }

        [Test]
        public void GetNumberOfPags_Counts_Response_Pages()
        {
            string xml = "<SurveyResponse><Page PageNumber=\"1\" /><Page PageNumber=\"2\" /></SurveyResponse>";

            Assert.That(SurveyHelper.GetNumberOfPags(xml), Is.EqualTo(2));
        }

        [Test]
        public void MergeXml_Replaces_The_Saved_Page()
        {
            var saved = XDocument.Parse("<SurveyResponse><Page PageNumber=\"1\"><ResponseDetail QuestionName=\"a\">old</ResponseDetail></Page><Page PageNumber=\"2\" /></SurveyResponse>");
            var current = XDocument.Parse("<Page PageNumber=\"1\"><ResponseDetail QuestionName=\"a\">new</ResponseDetail></Page>");

            XDocument merged = SurveyHelper.MergeXml(saved, current, 1);

            Assert.That(merged.Root.Elements("Page"), Has.Exactly(2).Items);
            Assert.That(merged.ToString(), Does.Contain(">new<").And.Not.Contain(">old<"));
        }

        [Test]
        public void MergeXml_Appends_A_New_Page()
        {
            var saved = XDocument.Parse("<SurveyResponse><Page PageNumber=\"1\" /></SurveyResponse>");
            var current = XDocument.Parse("<Page PageNumber=\"2\"><ResponseDetail QuestionName=\"b\">x</ResponseDetail></Page>");

            XDocument merged = SurveyHelper.MergeXml(saved, current, 2);

            Assert.That(merged.Root.Elements("Page"), Has.Exactly(2).Items);
        }

        [Test]
        public void UpdateControlsValues_Sets_Response_By_Case_Insensitive_Field_Name()
        {
            var form = FormProvider.GetForm(SurveyFixtures.SurveyInfoDto(), 1, SurveyFixtures.SurveyAnswer());

            SurveyHelper.UpdateControlsValues(form, "TEXTBOX2", "synthetic value");

            Assert.That(((MvcDynamicForms.Fields.InputField)form.Fields["textbox2"]).Response, Is.EqualTo("synthetic value"));
            Assert.That(((MvcDynamicForms.Fields.InputField)form.Fields["textbox1"]).Response, Is.Null.Or.Empty);
        }
    }
}
