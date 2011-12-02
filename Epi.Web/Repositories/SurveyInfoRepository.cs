﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Epi.Web.Repositories.Core;
using Epi.Web.ActionServiceClient;

namespace Epi.Web.Repositories
{
    public class SurveyInfoRepository : RepositoryBase, ISurveyInfoRepository
    {


        /// <summary>
        /// Calling the proxy client to fetch a SurveyInfoDTO object
        /// </summary>
        /// <param name="surveyid"></param>
        /// <returns></returns>
        public Common.DTO.SurveyInfoDTO GetSurveyInfoById(string surveyid)
        {
            
            Epi.Web.Common.DTO.SurveyInfoDTO surveyInfoDTO = Client.GetSurveyInfoById(surveyid);
            
            return surveyInfoDTO;
            
        }

        #region stubcode
            public List<Common.DTO.SurveyInfoDTO> GetList(Criterion criterion = null)
            {
                throw new NotImplementedException();
            }

            public Common.DTO.SurveyInfoDTO Get(int id)
            {
                throw new NotImplementedException();
            }

            public int GetCount(Criterion criterion = null)
            {
                throw new NotImplementedException();
            }

            public void Insert(Common.DTO.SurveyInfoDTO t)
            {
                throw new NotImplementedException();
            }

            public void Update(Common.DTO.SurveyInfoDTO t)
            {
                throw new NotImplementedException();
            }

            public void Delete(int id)
            {
                throw new NotImplementedException();
            } 
        #endregion
       
    }
}