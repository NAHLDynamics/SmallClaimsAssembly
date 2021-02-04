//using System;
//using System.Activities;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Runtime.Serialization;
//using System.Runtime.Serialization.Json;
//using System.Text;
//using System.Threading.Tasks;

////SDK D365 Assemblies
//using Microsoft.Crm.Sdk.Messages;
//using Microsoft.Xrm.Sdk;
//using Microsoft.Xrm.Sdk.Query;
//using Microsoft.Xrm.Sdk.Workflow;


//namespace SmallClaimsAssembly
//{
//    public class ValidateClaim : CodeActivity
//    {
//        [Input("Regarding Claim Info:")]
//        [ReferenceTarget("pi_claiminformation")]
//        public InArgument<EntityReference> ClaimInformation { get; set; }

//        [Output("Success")]
//        public OutArgument<Boolean> Success { get; set; }

//        [Output("Result")]
//        public OutArgument<string> ReturnString { get; set; }

//        public string returnstring;

//        protected override void Execute(CodeActivityContext context)
//        {
//            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
//            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
//            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

//            EntityReference claiminforeference = context.GetValue(this.ClaimInformation);

//            bool success;
//            try
//            {

//                #region Query (Claim Info - Claimant - Case Assessment)

//                QueryExpression claimQuery = new QueryExpression("pi_claiminformation");
//                claimQuery.Criteria.AddCondition(new ConditionExpression("pi_claiminformationid", ConditionOperator.Equal, claiminforeference.Id));
//                claimQuery.ColumnSet = new ColumnSet(true);

//                LinkEntity contactlink = new LinkEntity("pi_claiminformation", "contact", "pi_claimant", "contactid", JoinOperator.LeftOuter);
//                contactlink.Columns = new ColumnSet(true);
//                contactlink.EntityAlias = "co";
//                claimQuery.LinkEntities.Add(contactlink);

//                LinkEntity caseassessmentlink = new LinkEntity("pi_claiminformation", "nal_caseassessment", "nal_caseassessment", "nal_caseassessmentid", JoinOperator.LeftOuter);
//                caseassessmentlink.Columns = new ColumnSet(true);
//                caseassessmentlink.EntityAlias = "ca";
//                claimQuery.LinkEntities.Add(caseassessmentlink);

//                Entity claiminformation = service.RetrieveMultiple(claimQuery).Entities.FirstOrDefault();

//                #endregion

//                #region Top end Case information
//                returnstring = constructReturn(claiminformation.Contains("pi_claimantreferralsource"), "Claim Information - Claimant Referral Source is Mandatory");

//                if(claiminformation.GetAttributeValue<string>("pi_claimantreferralsource") == "157430006")
//                    returnstring = constructReturn(claiminformation.Contains("pi_referralothersource"), "Claim Information - Other Source Text is Mandatory when Claimant Referral Source is 'Other Source'");

//                returnstring = constructReturn(claiminformation.Contains("pi_referencenumber"), "Claim Information - Reference Number is Mandatory");

//                #endregion

//                #region Claimant Information
                
//                returnstring = constructReturn(claiminformation.Contains("co.salutation"), "Claimant Contact - Title/Salutation is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("co.firstname"), "Claimant Contact - First Name is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("co.lastname"), "Claimant Contact - Last Name is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("co.birthdate"), "Claimant Contact - Birth Date is Mandatory");

//                returnstring = constructReturn(claiminformation.Contains("co.address2_line1"), "Claimant Contact - Address line One is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("co.address2_city"), "Claimant Contact - City is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("co.address2_country"), "Claimant Contact - Country is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("co.address2_postalcode"), "Claimant Contact - Postal Code is Mandatory");

//                returnstring = constructReturn(claiminformation.Contains("co.emailaddress1"), "Claimant Contact - Email Address is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("co.mobilephone") || claiminformation.Contains("co.telephone2"), "Claimant Contact - Phone Number is Mandatory");

//                if(!claiminformation.Contains("ca.nal_ninumber"))
//                    returnstring = constructReturn(claiminformation.Contains("ca.nal_ninumber"), "Case Assessment - Reason No NI Number is Mandatory if no NI Number present");

//                /* Need new fields for this
//                 * personalInformation.nino.employmentStatus
//                 * personalInformation.nino.employmentDescription
//                 * 
//                 * signatoryDetails.signatoryFirstName
//                 * signatoryDetails.signatoryMiddleName
//                 * signatoryDetails.signatoryLastName
//                 */

//                #endregion

//                #region Accident Information

//                returnstring = constructReturn(claiminformation.Contains("pi_clientsstatusinaccident"), "Claim Information - Client status in accident is Mandatory");
//                returnstring = constructReturn(claiminformation.Contains("pi_askcuepi"), "Claim Information - AskcuePI is Mandatory");

//                returnstring = constructReturn(claiminformation.Contains("pi_datetimeofaccident"), "Claim Information - Date and Time of Accident is Mandatory");
                
//                accident.accidentType
//                accident.accidentDescription
//                accident.isDashcamEvidence


//                #endregion

//                success = true;
//            }
//            catch (Exception ex)
//            {
//                //Console.WriteLine(ex.Message);
//                returnstring = ex.Message;
//                success = false;
//            }


//            this.Success.Set(context, success);
//            this.ReturnString.Set(context, returnstring);

//        }

//        public string constructReturn(bool contains, string error) => contains ? returnstring : returnstring + "\n" + error;
//    }
//}
