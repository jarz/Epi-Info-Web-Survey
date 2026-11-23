using System;
using System.Collections.Generic;
using Epi.Web.Common.DTO;
using Epi.Web.Common.Message;
using Epi.Web.MVC.Facade;
using Epi.Web.MVC.Models;
using Epi.Web.MVC.Repositories.Core;

namespace Epi.Web.MVC.Test.Fakes
{
    /// <summary>
    /// Hand-written ISurveyFacade double. Only the read paths used by controller entry checks are
    /// configurable; every other member throws so a test that reaches unexpected service code fails loudly.
    /// </summary>
    public sealed class FakeSurveyFacade : ISurveyFacade
    {
        public SurveyInfoModel SurveyInfo { get; set; }
        public SurveyAnswerDTO SurveyAnswer { get; set; }
        public List<string> RequestedSurveyIds { get; } = new List<string>();
        public List<string> RequestedResponseIds { get; } = new List<string>();

        public SurveyInfoModel GetSurveyInfoModel(string surveyId)
        {
            RequestedSurveyIds.Add(surveyId);
            if (SurveyInfo == null)
            {
                throw new InvalidOperationException("No SurveyInfo configured for the fake facade.");
            }
            return SurveyInfo;
        }

        public SurveyAnswerResponse GetSurveyAnswerResponse(string responseId, string SurveyId = "", int UserId = 0)
        {
            RequestedResponseIds.Add(responseId);
            var response = new SurveyAnswerResponse();
            if (SurveyAnswer != null)
            {
                response.SurveyResponseList.Add(SurveyAnswer);
            }
            return response;
        }

        private static NotSupportedException NotUsed(string member)
        {
            return new NotSupportedException("FakeSurveyFacade." + member + " is not expected to be called by these tests.");
        }

        public MvcDynamicForms.Form GetSurveyFormData(string surveyId, int pageNumber, SurveyAnswerDTO surveyAnswerDTO, bool IsMobileDevice, List<SurveyAnswerDTO> _SurveyAnswerDTOList = null, List<FormsHierarchyDTO> FormsHierarchyDTOList = null, bool IsAndroid = false, bool SetAllDropDownValues = true) { throw NotUsed(nameof(GetSurveyFormData)); }
        public SurveyAnswerDTO CreateSurveyAnswer(string surveyId, string responseId, int UserId = 0, bool IsChild = false, string RelateResponseId = "", bool IsEditMode = false, int CurrentOrgId = -1) { throw NotUsed(nameof(CreateSurveyAnswer)); }
        public void UpdateSurveyResponse(SurveyInfoModel surveyInfoModel, string responseId, MvcDynamicForms.Form form, SurveyAnswerDTO surveyAnswerDTO, bool IsSubmited, bool IsSaved, int PageNumber, int UserId = -1) { throw NotUsed(nameof(UpdateSurveyResponse)); }
        public UserAuthenticationResponse ValidateUser(string responseId, string passcode) { throw NotUsed(nameof(ValidateUser)); }
        public void UpdatePassCode(string responseId, string passcode) { throw NotUsed(nameof(UpdatePassCode)); }
        public UserAuthenticationResponse GetAuthenticationResponse(string responseId) { throw NotUsed(nameof(GetAuthenticationResponse)); }
        public ISurveyAnswerRepository GetSurveyAnswerRepository() { throw NotUsed(nameof(GetSurveyAnswerRepository)); }
        public OrganizationAccountResponse CreateAccount(OrganizationAccountRequest AccountRequest) { throw NotUsed(nameof(CreateAccount)); }
        public SurveyAnswerResponse GetFormResponseList(SurveyAnswerRequest FormResponseReq) { throw NotUsed(nameof(GetFormResponseList)); }
        public OrganizationAccountResponse GetStateList(OrganizationAccountRequest Request) { throw NotUsed(nameof(GetStateList)); }
        public SurveyControlsResponse GetSurveyControlList(SurveyControlsRequest pRequestMessage) { throw NotUsed(nameof(GetSurveyControlList)); }
        public SurveyInfoResponse PublishExcelSurvey(SurveyInfoRequest Request) { throw NotUsed(nameof(PublishExcelSurvey)); }
        public SurveyAnswerResponse GetSurveyAnswerResponse(SurveyAnswerRequest responseId) { throw NotUsed("GetSurveyAnswerResponse(SurveyAnswerRequest)"); }
        public bool ValidateOrganization(OrganizationRequest Request) { throw NotUsed(nameof(ValidateOrganization)); }
        public SurveyInfoResponse GetAllSurveysByOrgKey(string OrgKey) { throw NotUsed(nameof(GetAllSurveysByOrgKey)); }
        public OrganizationAccountResponse GetUserOrgId(OrganizationAccountRequest Request) { throw NotUsed(nameof(GetUserOrgId)); }
        public SourceTablesResponse GetSourceTables(string surveyId) { throw NotUsed(nameof(GetSourceTables)); }
        public SurveyInfoResponse GetChildFormInfo(SurveyInfoRequest SurveyInfoRequest) { throw NotUsed(nameof(GetChildFormInfo)); }
        public FormsHierarchyResponse GetFormsHierarchy(FormsHierarchyRequest FormsHierarchyRequest) { throw NotUsed(nameof(GetFormsHierarchy)); }
        public SurveyAnswerResponse GetSurveyAnswerHierarchy(SurveyAnswerRequest pRequest) { throw NotUsed(nameof(GetSurveyAnswerHierarchy)); }
        public void UpdateResponseStatus(SurveyAnswerRequest Request) { throw NotUsed(nameof(UpdateResponseStatus)); }
        public bool HasResponse(string SurveyId, string ResponseId) { throw NotUsed(nameof(HasResponse)); }
        public SurveyAnswerResponse SetChildRecord(SurveyAnswerRequest surveyAnswerRequest) { throw NotUsed(nameof(SetChildRecord)); }
        public SurveyAnswerResponse DeleteResponse(SurveyAnswerRequest sARequest) { throw NotUsed(nameof(DeleteResponse)); }
        public SurveyAnswerResponse GetResponsesByRelatedFormId(SurveyAnswerRequest formResponseReq) { throw NotUsed(nameof(GetResponsesByRelatedFormId)); }
        public void SaveSurveyAnswer(SurveyAnswerRequest pRequest) { throw NotUsed(nameof(SaveSurveyAnswer)); }
        public string GetSurveyResponseJson(SurveyAnswerDTO surveyAnswerDTO, List<FormsHierarchyDTO> FormsHierarchyDTOList, SurveyControlsResponse List) { throw NotUsed(nameof(GetSurveyResponseJson)); }
        public OrganizationAccountResponse GetOrg(OrganizationAccountRequest Request) { throw NotUsed(nameof(GetOrg)); }
        public bool SetJsonColumn(string Json, string Responseid) { throw NotUsed(nameof(SetJsonColumn)); }
        public DashboardResponse GetSurveyDashboardInfo(string SurveyId) { throw NotUsed(nameof(GetSurveyDashboardInfo)); }
        public bool UpdateSourceTable(SourceTablesRequest sourceTablesRequest) { throw NotUsed(nameof(UpdateSourceTable)); }
        public PublishReportResponse GetSurveyReportList(PublishReportRequest publishReportRequest) { throw NotUsed(nameof(GetSurveyReportList)); }
        public PublishReportResponse GetSurveyReport(PublishReportRequest publishReportRequest) { throw NotUsed(nameof(GetSurveyReport)); }
    }
}
