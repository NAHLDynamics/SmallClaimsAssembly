using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

//SDK D365 Assemblies
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;


namespace SmallClaimsAssembly
{
    public class LiabilityReply : CodeActivity
    {
        
        [Input("Regarding Small Claim:")]
        [ReferenceTarget("nal_smallclaiminformation")]
        public InArgument<EntityReference> SmallClaim { get; set; }

        [Input("Regarding Small Claim Liability:")]
        [ReferenceTarget("nal_smallclaimsliability")]
        public InArgument<EntityReference> SmallClaimsLiability { get; set; }

        [Input("Liability Response:")]
        [AttributeTarget("nal_smallclaimsliability", "nal_liabilityresponse")]
        public InArgument<OptionSetValue> LiabilityResponse { get; set; }
        
        [Input("Challenge Liability Claim ID:")]
        public InArgument<string> ChallengeLiabilityID { get; set; }

        [Output("Success")]
        public OutArgument<Boolean> Success { get; set; }

        [Output("Result")]
        public OutArgument<string> RetString { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService traceService = context.GetExtension<ITracingService>();

            EntityReference smallclaimsliability = context.GetValue(this.SmallClaimsLiability);
            EntityReference smallclaims = context.GetValue(this.SmallClaim);
            OptionSetValue liabilityresponse = context.GetValue(this.LiabilityResponse);
            string challengeid = context.GetValue(this.ChallengeLiabilityID);

            bool success = false;
            string retstring = "";

            #region Query (API Settings)

            QueryExpression settingsQuery = new QueryExpression("nal_apisettings");
            settingsQuery.Criteria.AddCondition(new ConditionExpression("nal_apisettingsid", ConditionOperator.Equal, "4F63E58E-8655-EB11-8128-005056B21276"));
            settingsQuery.ColumnSet = new ColumnSet(true);

            Entity APISettings = service.RetrieveMultiple(settingsQuery).Entities.FirstOrDefault();

            #endregion

            try
            {
                if (APISettings != null)
                {
                    string requesturl = APISettings.GetAttributeValue<string>("nal_url");
                    string orgurl = APISettings.GetAttributeValue<string>("nal_orgurl");

                    string fullrequesturl = "";
                    if (liabilityresponse.Value == 808850002 /*Liability denied*/)
                    {
                        fullrequesturl = requesturl + "/api/smallclaims/disputeliability/" + orgurl + "/" + smallclaimsliability.Id + "/" + smallclaims.Id;
                    }
                    else if (liabilityresponse.Value == 808850001 /*Liability admitted in part*/)
                    {
                        if (challengeid == null) /*"Challenge Partial Liability*/
                        {
                            fullrequesturl = requesturl + "/api/smallclaims/challengepartialliability/" + orgurl + "/" + smallclaimsliability.Id + "/" + smallclaims.Id;
                        }
                        else /*"Review Partial Liability*/
                        {
                            fullrequesturl = requesturl + "/api/smallclaims/reviewpartialliability/" + orgurl + "/" + smallclaimsliability.Id + "/" + smallclaims.Id;
                        }
                    }
                    else
                    {
                        throw new Exception("Unable to identify Liability Response Type");
                    }

                    WebRequest request = WebRequest.Create(fullrequesturl);
                    request.Method = "GET";
                    WebResponse response = request.GetResponse();

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        retstring = result;
                    }

                    success = true;
                }

            }
            catch (Exception ex)
            {
                retstring = ex.Message;
                success = false;
            }


            this.Success.Set(context, success);
            this.RetString.Set(context, retstring);

        }


    }
}
