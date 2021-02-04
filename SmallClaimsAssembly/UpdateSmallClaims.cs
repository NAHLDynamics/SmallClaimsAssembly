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
    public class UpdateSmallClaims : CodeActivity
    {
        [Input("Case Assessment:")]
        [ReferenceTarget("nal_caseassessment")]
        public InArgument<EntityReference> CaseAssessment { get; set; }

        [Input("Small Claim Info:")]
        [ReferenceTarget("nal_smallclaiminformation")]
        public InArgument<EntityReference> SmallClaims { get; set; }

        [Output("Success")]
        public OutArgument<Boolean> Success { get; set; }

        [Output("Errors")]
        public OutArgument<string> Errors { get; set; }

        public Entity smallClaimsUpdate;
        public ITracingService traceService;
        public string errors = "";

        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            traceService = context.GetExtension<ITracingService>();


            #region Query (Case Assessment)

            EntityReference caseAssessmentReference = context.GetValue(this.CaseAssessment);

            QueryExpression caseQuery = new QueryExpression("nal_caseassessment");
            caseQuery.Criteria.AddCondition(new ConditionExpression("nal_caseassessmentid", ConditionOperator.Equal, caseAssessmentReference.Id));
            caseQuery.ColumnSet = new ColumnSet(true);

            Entity caseassessment = service.RetrieveMultiple(caseQuery).Entities.FirstOrDefault();

            traceService.Trace("Case Assessment Ref ID => " + caseAssessmentReference.Id + "\n");
            
            errors += "Case Assessment Ref ID => " + caseAssessmentReference.Id + "\n";

            #endregion

            #region Query (Small Claims)

            EntityReference smallClaimsReference = context.GetValue(this.SmallClaims);

            QueryExpression smallclaimQuery = new QueryExpression("nal_smallclaiminformation");
            smallclaimQuery.Criteria.AddCondition(new ConditionExpression("nal_smallclaiminformationid", ConditionOperator.Equal, smallClaimsReference.Id));
            smallclaimQuery.ColumnSet = new ColumnSet(true);

            Entity smallclaims = service.RetrieveMultiple(smallclaimQuery).Entities.FirstOrDefault();

            traceService.Trace("Small claims Ref ID => " + smallClaimsReference.Id + "\n");

            errors += "Small claims Ref ID => " + smallClaimsReference.Id + "\n";

            #endregion

            traceService.Trace("Try find -> Case Assessment and Small Claims!");
            if (caseassessment != null && smallclaims != null)
            {

                traceService.Trace("Case Assessment and Small Claims found!");

                smallClaimsUpdate = new Entity("nal_smallclaiminformation", smallClaimsReference.Id);

                string[][] mapping = getMapping();

                for (int n = 0; n < mapping[0].Count(); n++) {
                    try { 
                        checkAndUdpate(caseassessment, smallclaims, mapping[0][n], mapping[1][n], service);
                    }
                    catch(Exception ex)
                    {
                        errors += "Error => " + ex.Message + " fields[" + mapping[0][n] + "][" + mapping[1][n] + "]\n";
                        traceService.Trace("Error => " + ex.Message + " fields[" + mapping[0][n] + "][" + mapping[1][n] + "]\n");
                    }
                }

                traceService.Trace("Service.Update");
                service.Update(smallClaimsUpdate);
                this.Errors.Set(context, errors);

            }
            else
            {
                this.Errors.Set(context, errors + "One or more entities not found!");
            }

        }

        private void checkAndUdpate(Entity from, Entity to, string attributeFrom, string attributeTo, IOrganizationService service)
        {
            
            string[] CalculatedFieldList = getCalculatedFieldList();

            try
            {
                if (from.Contains(attributeFrom))
                {
                    if (to.Contains(attributeTo))
                    {
                        if (CalculatedFieldList.Contains(attributeTo))
                        {
                            if (attributeTo == "nal_employmentdescription")
                                if (to.Attributes[attributeTo].ToString() != ((EntityReference)from.Attributes[attributeFrom]).Name.ToString())
                                    smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name.ToString();
                            if(attributeTo == "nal_isregistrationknown")
                                if(from.Contains("nal_rta_vehicleregistrationnumber"))
                                    if (((OptionSetValue)to.Attributes[attributeTo]).Value != 808850000)
                                        smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                            if(attributeTo == "nal_enterthehospitalyourclientattended")
                                if (from.Contains("nal_didyouattendthehospital") && from.Attributes["nal_didyouattendthehospital"].ToString() == "True")
                                {
                                    QueryExpression hospitalQuery = new QueryExpression("pi_hospitalattended");
                                    hospitalQuery.Criteria.AddCondition(new ConditionExpression("nal_caseassessmentref", ConditionOperator.Equal, from.Id));
                                    hospitalQuery.ColumnSet = new ColumnSet("pi_name");
                                    hospitalQuery.AddOrder("createdon", OrderType.Descending);

                                    Entity hospitals = service.RetrieveMultiple(hospitalQuery).Entities.FirstOrDefault();

                                    if(hospitals != null)
                                    {
                                        if(to.Attributes[attributeTo].ToString() != hospitals.Attributes["pi_name"].ToString())
                                            to.Attributes[attributeTo] = hospitals.Attributes["pi_name"].ToString();
                                    }

                                }
                            if (attributeTo == "nal_nameandaddressofpolicestation")
                                if (to.Attributes[attributeTo].ToString() != ((EntityReference)from.Attributes[attributeFrom]).Name.ToString())
                                    smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name.ToString();
                        }
                        else
                        {

                            switch (from.Attributes[attributeFrom].ToString())
                            {
                                case ("Microsoft.Xrm.Sdk.EntityReference"):
                                    if (((EntityReference)from.Attributes[attributeFrom]).Id.ToString() != ((EntityReference)to.Attributes[attributeTo]).Id.ToString())
                                        smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    break;
                                case ("Microsoft.Xrm.Sdk.Money"):
                                    if (((Money)from.Attributes[attributeFrom]).ToString() != ((Money)to.Attributes[attributeTo]).ToString())
                                        smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    break;
                                case ("Microsoft.Xrm.Sdk.OptionSetValue"):
                                    if (((OptionSetValue)from.Attributes[attributeFrom]).Value < 2)
                                    {
                                        if (((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() == "0" && ((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() != "808850001")
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850001);
                                        }
                                        else if (((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() == "1" && ((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() != "808850000")
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                                        }
                                    }
                                    else
                                    {
                                        if (((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() != ((OptionSetValue)to.Attributes[attributeTo]).Value.ToString())
                                            smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    }
                                    break;
                                case ("True"):
                                    if (((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() != "808850000")
                                        smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                                    break;
                                case ("False"):
                                    if (((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() != "808850001")
                                        smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850001);
                                    break;
                                default:
                                    if (from.Attributes[attributeFrom].ToString() != ((EntityReference)to.Attributes[attributeTo]).ToString())
                                        smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (CalculatedFieldList.Contains(attributeTo))
                        {
                            if (attributeTo == "nal_employmentdescription")
                                smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name.ToString();
                            if (attributeTo == "nal_isregistrationknown")
                                if (from.Contains("nal_rta_vehicleregistrationnumber"))
                                    smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                            if (attributeTo == "nal_enterthehospitalyourclientattended")
                                if (from.Contains("nal_didyouattendthehospital") && from.Attributes["nal_didyouattendthehospital"].ToString() == "True")
                                {
                                    QueryExpression hospitalQuery = new QueryExpression("pi_hospitalattended");
                                    hospitalQuery.Criteria.AddCondition(new ConditionExpression("nal_caseassessmentref", ConditionOperator.Equal, from.Id));
                                    hospitalQuery.ColumnSet = new ColumnSet("pi_name");
                                    hospitalQuery.AddOrder("createdon", OrderType.Descending);

                                    Entity hospitals = service.RetrieveMultiple(hospitalQuery).Entities.FirstOrDefault();

                                    if (hospitals != null)
                                            to.Attributes[attributeTo] = hospitals.Attributes["pi_name"].ToString();
                                }
                            if (attributeTo == "nal_nameandaddressofpolicestation")
                                smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name.ToString();

                        }
                        else
                        {

                            switch (from.Attributes[attributeFrom].ToString())
                            {
                                case ("Microsoft.Xrm.Sdk.EntityReference"):
                                    smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    break;
                                case ("Microsoft.Xrm.Sdk.Money"):
                                    smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    break;
                                case ("Microsoft.Xrm.Sdk.OptionSetValue"):
                                    if (((OptionSetValue)from.Attributes[attributeFrom]).Value < 2)
                                    {
                                        if (((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() == "0")
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850001);
                                        }
                                        else if (((OptionSetValue)from.Attributes[attributeFrom]).Value.ToString() == "1")
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                                        }
                                    }
                                    else
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    }
                                    break;
                                case ("True"):
                                    smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                                    break;
                                case ("False"):
                                    smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850001);
                                    break;
                                default:
                                    smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                    break;
                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                traceService.Trace(string.Format("Case Assessment Field[{0}] and Small Claims Field[{0}] where not able to be mapped!\n", attributeFrom, attributeTo));
            }
        }

        private string[][] getMapping() => new string[][]
        {
            new string[]{"nal_rta_busdriverid","nal_caseassessmentaccidentdescription","nal_accidentlocation","nal_accidentlocation","nal_accidenttype","nal_askcuereference","nal_busdrivercontact","nal_rta_busroutenumber","nal_customercontact","nal_accidentdateandtime","nal_defendantdetails","nal_defendantvehiclecolour","nal_defendantvehiclemake","nal_defendantvehiclemodel","nal_defendantvehicleregistration","nal_haveyousoughtanymedicalattention","nal_didyouattendthegp","nal_rta_evidenceofbustravel","nal_otherdriverdetails","nal_occupation","nal_descriptionofthedefendant","nal_hasmedicalprofessionalrecommendedrehab","nal_sexofdefendant","nal_haveyousoughtanymedicalattention","nal_police_wastheincidentreportedtothepolice","nal_howwerethedefendantsdetailsobtained","nal_howmuchtimeoffhaveyouhaddays","nal_wereyoukeptinthehospitalovernight","nal_areyoustilloff","nal_rta_makeofvehicle","nal_rta_modelofvehicle","nal_police_nameofreportingofficer","nal_vehiclehirecompany","nal_rta_howmanypeoplewereinyourvehicle","nal_vehicleowner","nal_police_referencereferencenumber","nal_rta_vehicleregistrationnumber","nal_takenanytimeoffasaresultoftheinjury","nal_rta_wereyouwearingaseatbelt","nal_didyouattendthehospital", "nal_werethereanywitnesses"},
            new string[]{"nal_busdriveridnumber","nal_accidentdescription","nal_accidentlocationaddress","nal_accidentlocationdescription","nal_accidenttype","nal_askcuepi","nal_busdriversname","nal_busroute","nal_claimantid","nal_datetimeofaccident","nal_defendantid","nal_defendantvehiclecolour","nal_defendantvehiclemake","nal_defendantvehiclemodel","nal_defendantvehiclevrm","nal_didclientseekanyotherclinicalattendance","nal_didyourclientseeagp","nal_doesclienthaveevidenceoftravel","nal_driver","nal_employmentdescription","nal_enterclientsdescriptionofthedriver","nal_furthertreatmentrecommended","nal_genderofdriver","nal_hasclientsoughtmedicalattention","nal_hastheaccidentbeenreportedtothepolice","nal_howdidclientobtainthesedetails","nal_howmanydaysintotalwasyourclientoffwork","nal_howmanynightsdidyourclientstayinhospital","nal_isclientstilloffwork","nal_make","nal_model","nal_nameofreportingofficer","nal_nameofthehirecompany","nal_numberofpassangers","nal_ownerofthecarifnotclient","nal_policereferencenumber","nal_registrationnumberofthevehicleclientwasin","nal_takentimeoffworkduetoinjury","nal_wasclientwearingaseatbelt","nal_washospitalattended", "nal_werethereanywitnessesattheaccident" }
        };

        private string[] getCalculatedFieldList() => new string[] { "nal_employmentdescription", "nal_isregistrationknown", "nal_enterthehospitalyourclientattended", "nal_nameandaddressofpolicestation"};

    }
}
