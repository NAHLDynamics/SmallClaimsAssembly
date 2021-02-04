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
    public class CWAIntegrationTest : CodeActivity
    {
        [Input("Regarding Case:")]
        [ReferenceTarget("nal_caseassessment")]
        public InArgument<EntityReference> CaseAssessment { get; set; }

        [Output("Success")]
        public OutArgument<Boolean> Success { get; set; }

        [Output("Result")]
        public OutArgument<string> RetString { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            EntityReference matter = context.GetValue(this.CaseAssessment);

            bool success = false;
            string retstring = "";
            try
            {
                WebRequest request = WebRequest.Create("https://nahlidcheckapi-dev.azurewebsites.net/api/idcheck/nahldev/F9FB370B-6A2D-EB11-8124-005056B21276/1FFD69CA-0791-E911-8100-005056B21276/Send for ID Check");
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
