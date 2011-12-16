﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epi.Web.Common.BusinessObject;
using Epi.Web.Common.DTO;

namespace Epi.Web.EF
{
    /// <summary>
    /// Maps Entity Framework entities to business objects and vice versa.
    /// </summary>
    public class Mapper
    {
        /// <summary>
        /// Maps SurveyMetaData entity to SurveyInfoBO business object.
        /// </summary>
        /// <param name="entity">A SurveyMetaData entity to be transformed.</param>
        /// <returns>A SurveyInfoBO business object.</returns>
        internal static SurveyInfoBO Map(SurveyMetaData entity)
        {
            return new SurveyInfoBO
            {
                SurveyId = entity.SurveyId.ToString(),
                SurveyName = entity.SurveyName,
                SurveyNumber = entity.SurveyNumber,
                XML = entity.TemplateXML,
                IntroductionText = entity.IntroductionText,
                OrganizationName = entity.OrganizationName,
                DepartmentName = entity.DepartmentName,
                ClosingDate = entity.ClosingDate,
                SurveyType =   entity.SurveyTypeId 
            };
        }

        /// <summary>
        /// Maps SurveyInfoBO business object to SurveyMetaData entity.
        /// </summary>
        /// <param name="businessobject">A SurveyInfoBO business object.</param>
        /// <returns>A SurveyMetaData entity.</returns>
        internal static SurveyMetaData Map(SurveyInfoBO businessobject)
        {
            return new SurveyMetaData
            {
                SurveyId = new Guid(businessobject.SurveyId),
                SurveyName = businessobject.SurveyName,
                SurveyNumber = businessobject.SurveyNumber,
                TemplateXML = businessobject.XML,
                IntroductionText = businessobject.IntroductionText,
                OrganizationName = businessobject.OrganizationName,
                DepartmentName = businessobject.DepartmentName,
                ClosingDate = businessobject.ClosingDate ,
                SurveyTypeId = businessobject.SurveyType 

            };
        }


        /// <summary>
        /// Maps SurveyMetaData entity to SurveyInfoBO business object.
        /// </summary>
        /// <param name="entity">A SurveyMetaData entity to be transformed.</param>
        /// <returns>A SurveyInfoBO business object.</returns>
        internal static SurveyResponseBO Map(SurveyResponseDTO entity)
        {
            return new SurveyResponseBO
            {
                SurveyId = entity.SurveyId.ToString(),
                ResponseId = entity.ResponseId.ToString(),
                XML = entity.XML,
                Status = entity.Status,
                DateLastUpdated = entity.DateLastUpdated,
                DateCompleted = entity.DateCompleted
            };
        }

        /// <summary>
        /// Maps SurveyInfoBO business object to SurveyMetaData entity.
        /// </summary>
        /// <param name="businessobject">A SurveyInfoBO business object.</param>
        /// <returns>A SurveyMetaData entity.</returns>
        internal static SurveyResponseDTO Map(SurveyResponseBO businessobject)
        {
            return new SurveyResponseDTO
            {
                SurveyId = businessobject.SurveyId,
                ResponseId = businessobject.ResponseId,
                XML = businessobject.XML,
                Status = businessobject.Status,
                DateLastUpdated = businessobject.DateLastUpdated,
                DateCompleted = businessobject.DateCompleted

            };
        }

        /// <summary>
        /// Maps SurveyMetaData entity to SurveyInfoBO business object.
        /// </summary>
        /// <param name="entity">A SurveyMetaData entity to be transformed.</param>
        /// <returns>A SurveyInfoBO business object.</returns>
        internal static SurveyResponseBO Map(SurveyResponse entity)
        {
            return new SurveyResponseBO
            {
                SurveyId = entity.SurveyId.ToString(),
                ResponseId = entity.ResponseId.ToString(),
                XML = entity.ResponseXML,
                Status = entity.StatusId,
                DateLastUpdated = entity.DateLastUpdated,
                DateCompleted = entity.DateCompleted.Value
            };
        }

        /// <summary>
        /// Maps SurveyInfoBO business object to SurveyMetaData entity.
        /// </summary>
        /// <param name="businessobject">A SurveyInfoBO business object.</param>
        /// <returns>A SurveyMetaData entity.</returns>
        internal static SurveyResponse ToEF(SurveyResponseBO pBO)
        {
            return new SurveyResponse
            {
                SurveyId = new Guid(pBO.SurveyId),
                ResponseId = new Guid(pBO.ResponseId),
                ResponseXML = pBO.XML,
                StatusId = pBO.Status,
                DateLastUpdated = pBO.DateLastUpdated,
                DateCompleted = pBO.DateCompleted

            };
        }
    }
}
