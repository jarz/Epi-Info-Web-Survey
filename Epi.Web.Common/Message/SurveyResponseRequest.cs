using System.Runtime.Serialization;
using Epi.Web.Common.MessageBase;
using Epi.Web.Common.Criteria;
using Epi.Web.Common.DTO;

namespace Epi.Web.Common.Message
{
    /// <summary>
    /// Represents a SurveyInfo request message from client.
    /// </summary>
    [DataContract(Namespace = "http://www.yourcompany.com/types/")]
    public class SurveyResponseRequest : RequestBase
    {
        public SurveyResponseRequest()
        {
            this.Criteria = new SurveyResponseCriteria();
        }

        /// <summary>
        /// Selection criteria and sort order
        /// </summary>
        [DataMember]
        public SurveyResponseCriteria Criteria;

        /// <summary>
        /// SurveyInfo object.
        /// </summary>
        [DataMember]
        public SurveyResponseDTO SurveyResponseDTO;
    }
}
