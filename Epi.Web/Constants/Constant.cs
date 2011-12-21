﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Epi.Web.MVC.Constants
{
    public static class Constant
    {
        public enum Status
        { 
           InProgress = 1,
           Complete = 2
        }

        /*sql commands*/
        public const string UPDATE = "Update";
        public const string CREATE = "Create";
        public const string SELECT = "Select";
        
        public const string SURVEY_ID = "SurveyId";
        public const string QUESTION_ID = "QuestionId";
        public const string RESPONSE_ID = "ResponseId";
        public const string CURRENT_PAGE = "CurrentPage";
        /*XML tags*/
        public const string SURVEY_RESPONSE = "SurveyResponse";
        public const string RESPONSE_DETAILS = "ResponseDetail";
        /*view names*/
        public const string INDEX_PAGE = "Index";
        public const string SURVEY_INTRODUCTION = "SurveyIntroduction";
        public const string SURVAY_PAGE = "Survey";
        public const string EXCEPTION_PAGE = "Exception";
        public const string SUBMIT_PAGE = "PostSubmit";

        /*controllers*/
        public const string HOME_CONTROLLER = "Home";
        public const string SURVEY_CONTROLLER = "Survey";

        /*action methods*/
        public const string INDEX = "Index";

        /*Survey page messages*/
        public const string SURVEY_NOT_EXISTS = "The Survey does not exists.";
        public const string SURVEY_SUBMISSION_MESSAGE = "Thank you! Your survey has been submitted.";
    }
}