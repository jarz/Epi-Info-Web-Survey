﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Epi.Web.Models
{
    
    /// <summary>
    /// The Survey Model that will be pumped to view
    /// </summary>
    public class SurveyResponseModel
    {
        public string ResponseId { get; set; }
        public string SurveyId { get; set; }
        public DateTime DateLastUpdated { get; set; }
        public DateTime DateCompleted { get; set; }
        public int Status { get; set; }
        public string XML { get; set; }

    }
}