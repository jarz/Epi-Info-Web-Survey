using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Epi.Web.Common.DTO;
using Epi.Web.Common.Json;
using Epi.Web.Common.Message;
using Epi.Web.MVC.Controllers;
using Epi.Web.MVC.Test.Fakes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Json
{
    /// <summary>Response JSON produced with Newtonsoft.Json 13.0.1 for survey data consumers.</summary>
    [TestFixture]
    public class SurveyResponseJsonTests
    {
        private static readonly Guid OrganizationKey = new Guid("1234abcd-0000-4000-8000-000000000000");

        private static SurveyControlsResponse Controls()
        {
            var controls = new SurveyControlsResponse();
            controls.SurveyControlList = new List<SurveyControlDTO>
            {
                new SurveyControlDTO { ControlId = "Age", ControlType = "NumericTextBox" },
                new SurveyControlDTO { ControlId = "Weight", ControlType = "NumericTextBox" },
                new SurveyControlDTO { ControlId = "Consent", ControlType = "CheckBox" },
                new SurveyControlDTO { ControlId = "Smoker", ControlType = "CheckBox" },
                new SurveyControlDTO { ControlId = "Name", ControlType = "TextBox" },
                new SurveyControlDTO { ControlId = "Notes", ControlType = "MultiLineTextBox" }
            };
            return controls;
        }

        private static List<FormsHierarchyDTO> RootHierarchy()
        {
            return new List<FormsHierarchyDTO>
            {
                new FormsHierarchyDTO
                {
                    FormId = SurveyFixtures.SurveyId,
                    IsRoot = true,
                    ResponseIds = new List<SurveyAnswerDTO>(),
                    SurveyInfo = new SurveyInfoDTO { SurveyId = SurveyFixtures.SurveyId, OrganizationKey = OrganizationKey }
                }
            };
        }

        private static JObject Serialize(SurveyAnswerDTO answer, List<FormsHierarchyDTO> hierarchy)
        {
            string json = new SurveyResponseJson().GetSurveyResponseJson(answer, hierarchy, Controls());
            return JObject.Parse(json);
        }

        [Test]
        public void Root_Response_Values_Are_Typed_By_Control()
        {
            var answer = SurveyFixtures.SurveyAnswer(1, "",
                Tuple.Create("Age", "42"),
                Tuple.Create("Weight", "71.5"),
                Tuple.Create("Consent", "Yes"),
                Tuple.Create("Smoker", "No"),
                Tuple.Create("Name", "Synthetic Person"),
                Tuple.Create("Notes", ""));

            JObject json = Serialize(answer, RootHierarchy());
            var qa = (JObject)json["ResponseQA"];

            Assert.That(qa["Age"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((long)qa["Age"], Is.EqualTo(42L));
            Assert.That(qa["Weight"].Type, Is.EqualTo(JTokenType.Float));
            Assert.That((decimal)qa["Weight"], Is.EqualTo(71.5m));
            Assert.That((bool)qa["Consent"], Is.True);
            Assert.That((bool)qa["Smoker"], Is.False);
            Assert.That((string)qa["Name"], Is.EqualTo("Synthetic Person"));
            Assert.That((string)qa["Notes"], Is.EqualTo(string.Empty));
        }

        [Test]
        public void Response_Identity_And_Organization_Key_Prefix_Are_Included()
        {
            JObject json = Serialize(SurveyFixtures.SurveyAnswer(1, "", Tuple.Create("Name", "x")), RootHierarchy());

            Assert.That((string)json["ResponseId"], Is.EqualTo(SurveyFixtures.ResponseId));
            Assert.That((string)json["FormId"], Is.EqualTo(SurveyFixtures.SurveyId));
            Assert.That((string)json["OKey"], Is.EqualTo("1234abcd"));
            Assert.That(json["ChildResponseDetailList"], Is.Empty);
            Assert.That(json.Properties(), Has.Exactly(8).Items);
        }

        [Test]
        public void Child_Responses_Are_Nested_With_Parent_Key()
        {
            var hierarchy = RootHierarchy();
            hierarchy.Add(new FormsHierarchyDTO
            {
                FormId = "child-form",
                IsRoot = false,
                ResponseIds = new List<SurveyAnswerDTO>
                {
                    new SurveyAnswerDTO
                    {
                        SurveyId = "child-form",
                        ResponseId = "child-response",
                        RelateParentId = SurveyFixtures.ResponseId,
                        ParentRecordId = SurveyFixtures.SurveyId,
                        XML = "<SurveyResponse><Page PageNumber=\"1\"><ResponseDetail QuestionName=\"Name\">Child</ResponseDetail></Page></SurveyResponse>"
                    }
                }
            });

            JObject json = Serialize(SurveyFixtures.SurveyAnswer(1, "", Tuple.Create("Name", "Parent")), hierarchy);
            var child = (JObject)json["ChildResponseDetailList"][0];

            Assert.That((string)child["ResponseId"], Is.EqualTo("child-response"));
            Assert.That((string)child["ParentResponseId"], Is.EqualTo(SurveyFixtures.ResponseId));
            Assert.That((string)child["ResponseQA"]["FKEY"], Is.EqualTo(SurveyFixtures.ResponseId));
            Assert.That((string)child["ResponseQA"]["ResponseId"], Is.EqualTo("child-response"));
            Assert.That((string)child["ResponseQA"]["Name"], Is.EqualTo("Child"));
        }

        [Test]
        public void Empty_Response_Xml_Produces_Empty_Json()
        {
            var answer = new SurveyAnswerDTO { ResponseId = SurveyFixtures.ResponseId, XML = null };

            Assert.That(new SurveyResponseJson().GetSurveyResponseJson(answer, RootHierarchy(), Controls()), Is.Empty);
        }

        [Test]
        public void Source_Table_Items_Become_A_Json_Array()
        {
            const string xml = "<SourceTables><SourceTable TableName=\"Colors\"><Item Code=\"1\" Name=\"Red\" /><Item Code=\"2\" Name=\"Blue\" /></SourceTable></SourceTables>";

            JArray items = JArray.Parse(SurveyResponseJson.XmlToJson(xml));

            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That((string)items[0]["@Name"], Is.EqualTo("Red"));
            Assert.That((string)items[1]["@Code"], Is.EqualTo("2"));
        }

        [Test]
        public void SerializeJsonIntoStream_Writes_Compact_Utf8_Without_Bom()
        {
            using (var stream = new MemoryStream())
            {
                SurveyController.SerializeJsonIntoStream(new { ResponseId = "r1", Count = 2, Text = "caf\u00e9" }, stream);

                byte[] bytes = stream.ToArray();
                Assert.That(bytes[0], Is.EqualTo((byte)'{'));
                Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo("{\"ResponseId\":\"r1\",\"Count\":2,\"Text\":\"caf\u00e9\"}"));
                Assert.That(stream.CanWrite, Is.True, "the caller's stream must be left open");
            }
        }
    }
}
