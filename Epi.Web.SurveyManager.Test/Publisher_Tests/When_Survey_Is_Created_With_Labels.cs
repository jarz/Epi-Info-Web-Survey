﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Epi.Web.BLL;
using Epi.Web.Common.BusinessObject;
using Epi.Web.Interfaces.DataInterfaces;
using Epi.Web.EF;
using System.Xml;

namespace Epi.Web.SurveyManager.Test.Publisher_Tests
{
    class When_Survey_Is_Created_With_Labels
    {
        [Test]
        public void Then_Labels_Should_be_Published() {
            //Arrange


            ISurveyInfoDao objISurveryInfoDao = new EntitySurveyInfoDao();
            SurveyInfo objSurveyInfo = new SurveyInfo(objISurveryInfoDao);

            Publisher objPublisher = new Publisher(objISurveryInfoDao);

            SurveyDataProvider DataObj = new SurveyDataProvider();//Get Data

            SurveyRequestBO objSurveyRequestBO;
            SurveyRequestResultBO objSurveyResponseBO;
            SurveyInfoBO objSurveyInfoBO;
            string TemplateXML;
            string surveyURL;
            string surveyID = string.Empty;
          

            //Act

            objSurveyRequestBO = DataObj.CreateSurveyRequestBOObject();


            TemplateXML = objSurveyRequestBO.TemplateXML;

            objSurveyResponseBO = objPublisher.PublishSurvey(objSurveyRequestBO);// publish survey and get Response back

            surveyURL = objSurveyResponseBO.URL;
            surveyID = surveyURL.Substring(surveyURL.LastIndexOf('/') + 1);  //Get the ID from Url. 


            objSurveyInfoBO = objSurveyInfo.GetSurveyInfoById(surveyID);


            //Assert
            Assert.NotNull(objSurveyInfoBO.XML);
            Assert.NotNull(objSurveyInfoBO.XML);
            //Assert.AreEqual(objSurveyInfoBO.XML, TemplateXML.ToString());

        
        }
    }
}