using System;
using System.IO;
using System.Text;
using Epi.Web.Common.DTO;
using Epi.Web.MVC.Models;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Fakes
{
    /// <summary>Synthetic survey metadata and responses; no production data.</summary>
    public static class SurveyFixtures
    {
        public const string SurveyId = "2e1d01d4-f50d-4f23-888b-cd4b7fc9884b";
        public const string ResponseId = "a3b4c5d6-0000-4000-8000-000000000001";

        public static string MetadataXml()
        {
            return File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Mock", "MetaDataXML.xml"));
        }

        public static SurveyInfoModel SurveyInfo(DateTime closingDate, bool isDraftMode = false)
        {
            return new SurveyInfoModel
            {
                SurveyId = SurveyId,
                SurveyName = "Synthetic Survey",
                IntroductionText = "Line one\r\nLine  two",
                ExitText = "Thank you\nfor  taking part",
                XML = "<Template />",
                ClosingDate = closingDate,
                StartDate = closingDate.AddYears(-1),
                IsDraftMode = isDraftMode
            };
        }

        public static SurveyInfoDTO SurveyInfoDto(bool isDraftMode = false)
        {
            return new SurveyInfoDTO
            {
                SurveyId = SurveyId,
                SurveyName = "Synthetic Survey",
                XML = MetadataXml(),
                ClosingDate = DateTime.Now.AddDays(30),
                IsDraftMode = isDraftMode
            };
        }

        public static string ResponseXml(string requiredFieldsList = "", params Tuple<string, string>[] answers)
        {
            var xml = new StringBuilder();
            xml.Append("<SurveyResponse SurveyId=\"").Append(SurveyId)
               .Append("\" LastPageVisited=\"1\" HiddenFieldsList=\"\" HighlightedFieldsList=\"\" DisabledFieldsList=\"\" RequiredFieldsList=\"")
               .Append(requiredFieldsList).Append("\" RecordBeforeFlag=\"\">");
            xml.Append("<Page PageNumber=\"1\" PageId=\"1\">");
            foreach (var answer in answers)
            {
                xml.Append("<ResponseDetail QuestionName=\"").Append(answer.Item1).Append("\">")
                   .Append(answer.Item2).Append("</ResponseDetail>");
            }
            xml.Append("</Page></SurveyResponse>");
            return xml.ToString();
        }

        public static SurveyAnswerDTO SurveyAnswer(int status = 1, string requiredFieldsList = "", params Tuple<string, string>[] answers)
        {
            return new SurveyAnswerDTO
            {
                ResponseId = ResponseId,
                SurveyId = SurveyId,
                Status = status,
                XML = ResponseXml(requiredFieldsList, answers)
            };
        }
    }
}
