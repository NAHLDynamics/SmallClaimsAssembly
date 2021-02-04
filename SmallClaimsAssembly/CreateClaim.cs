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
    public class CreateClaim : CodeActivity
    {

        [Input("Regarding Small Claim:")]
        [ReferenceTarget("nal_smallclaiminformation")]
        public InArgument<EntityReference> SmallClaim { get; set; }

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

            EntityReference smallclaim = context.GetValue(this.SmallClaim);

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
                if(APISettings != null)
                {
                    string requesturl = APISettings.GetAttributeValue<string>("nal_url");
                    string orgurl = APISettings.GetAttributeValue<string>("nal_orgurl");
                    
                    //requesturl + "/api/smallclaims/createclaim/" + orgurl + "/" + smallclaim.Id;
                    
                    WebRequest request = WebRequest.Create(requesturl + "/api/smallclaims/createclaim/" + orgurl + "/" + smallclaim.Id);
                    request.Method = "GET";
                    WebResponse response = request.GetResponse();

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        retstring = result;
                    }
                    //Console.WriteLine(response.Headers.GetValues.response.ToString());
                    //Console.WriteLine(response.ContentType);

                    success = true;
                }

            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                retstring = ex.Message;
                success = false;
            }


            this.Success.Set(context, success);
            this.RetString.Set(context, retstring);

        }
    }
}
