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

                for (int n = 0; n < mapping[0].Count(); n++)
                {
                    try
                    {
                        checkAndUpdate(caseassessment, smallclaims, mapping[0][n], mapping[1][n], service);
                        checkAndUpdateCalculatedFields(caseassessment, smallclaims, mapping[0][n], mapping[1][n], service);
                    }
                    catch (Exception ex)
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


        private void checkAndUpdate(Entity from, Entity to, string attributeFrom, string attributeTo, IOrganizationService service)
        {

            string[] CalculatedFieldList = getCalculatedFieldList();

            if (!CalculatedFieldList.Contains(attributeTo)) //not a calculated field
                try
                {
                    if (from.Contains(attributeFrom))
                    {
                        if (to.Contains(attributeTo))
                        {
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
                                        if (((OptionSetValue)to.Attributes[attributeTo]).Value.ToString() != "808850000")
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                                        break;
                                    case ("False"):
                                        if (((OptionSetValue)to.Attributes[attributeTo]).Value.ToString() != "808850001")
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850001);
                                        break;
                                    default:
                                        if (from.Attributes[attributeFrom].ToString() != to.Attributes[attributeTo].ToString())
                                            smallClaimsUpdate.Attributes[attributeTo] = from.Attributes[attributeFrom];
                                        break;
                                }
                                //service.Update(smallClaimsUpdate);
                            }
                        }

                        else
                        {
                            Console.WriteLine(from.Attributes[attributeFrom].ToString());

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
                            // service.Update(smallClaimsUpdate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    traceService.Trace(string.Format("Case Assessment Field[{0}] and Small Claims Field[{1}] where not able to be mapped!\n : Error{2}", attributeFrom, attributeTo, ex.Message + ":" + ex.StackTrace));
                }
        }

        private void checkAndUpdateCalculatedFields(Entity from, Entity to, string attributeFrom, string attributeTo, IOrganizationService service)
        {
            string[] CalculatedFieldList = getCalculatedFieldList();

            if (CalculatedFieldList.Contains(attributeTo)) // is a calculated field
            {
                try
                {
                    if (from.Contains(attributeFrom)) // small claims value is not present so  try and update it 
                    {
                        if (attributeTo == "nal_employmentdescription")
                        {
                            if (to.Contains(attributeTo))
                            {
                                if (to.Attributes[attributeTo].ToString() != ((EntityReference)from.Attributes[attributeFrom]).Name.ToString())
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name.ToString();
                                }

                            }
                            else
                            {
                                smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name.ToString();
                            }
                        }
                        if (attributeTo == "nal_isregistrationknown")
                            if (from.Contains("nal_rta_vehicleregistrationnumber"))
                            {
                                if (to.Attributes.Contains(attributeTo) && ((OptionSetValue)to.Attributes[attributeTo]).Value != 808850000)
                                    smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                                else
                                    smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000);
                            }
                        if (attributeTo == "nal_enterthehospitalyourclientattended")
                            if (from.Contains("nal_didyouattendthehospital") && from.Attributes["nal_didyouattendthehospital"].ToString() == "True")
                            {
                                QueryExpression hospitalQuery = new QueryExpression("pi_hospitalattended");
                                hospitalQuery.Criteria.AddCondition(new ConditionExpression("nal_caseassessmentref", ConditionOperator.Equal, from.Id));
                                hospitalQuery.ColumnSet = new ColumnSet("pi_name");
                                hospitalQuery.AddOrder("createdon", OrderType.Descending);

                                Entity hospitals = service.RetrieveMultiple(hospitalQuery).Entities.FirstOrDefault();

                                if (hospitals != null)
                                {
                                    var hospital = hospitals.Attributes["pi_name"].ToString();

                                    if (to.Attributes.Contains(attributeTo))
                                    {
                                        if (to.Attributes[attributeTo].ToString() != hospital)
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = hospitals.Attributes["pi_name"].ToString();
                                        }
                                    }
                                    else
                                        smallClaimsUpdate.Attributes[attributeTo] = hospitals.Attributes["pi_name"].ToString();
                                }

                            }
                        if (attributeTo == "nal_nameandaddressofpolicestation")
                        {
                            var policeStation = service.Retrieve("contact", ((EntityReference)from.Attributes[attributeFrom]).Id, new ColumnSet(true));
                            if (policeStation != null)
                            {
                                if (policeStation.Attributes.Contains("ptl_address1block"))
                                {
                                    var address = (policeStation.Attributes["fullname"].ToString() + "," + policeStation.Attributes["ptl_address1block"].ToString().Replace("\r\n", ",")).TrimEnd(',');
                                    if (to.Contains(attributeTo))
                                    {
                                        if (to.Attributes[attributeTo].ToString() != address)
                                            smallClaimsUpdate.Attributes[attributeTo] = address;
                                    }
                                    else { smallClaimsUpdate.Attributes[attributeTo] = address; }
                                }
                            }
                        }

                        if (attributeTo == "nal_claimantreferralsource")
                        {
                            if (to.Attributes.Contains(attributeTo))
                            {
                                if (((OptionSetValue)to.Attributes[attributeTo]).Value != 808850007)
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850007);
                                }
                            }
                            else
                                smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850007);
                        }

                        if (attributeTo == "nal_busdriversname")
                        {
                            if (to.Attributes.Contains(attributeTo))
                            {
                                if (to.Attributes[attributeTo].ToString() != from.FormattedValues[attributeFrom])
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = from.FormattedValues[attributeFrom];
                                }
                            }
                            else
                                smallClaimsUpdate.Attributes[attributeTo] = from.FormattedValues[attributeFrom];
                        }

                        if (attributeTo == "nal_clientinvolvement")
                        {
                            var subClaimType = ((OptionSetValue)(from.Attributes[attributeFrom])).Value;
                            if (subClaimType == 808850024 || subClaimType== 808850025 || subClaimType == 808850032 || subClaimType == 808850027) //driver // 25 32 27
                            {
                                if (to.Attributes.Contains(attributeTo))
                                {
                                    if (((OptionSetValue)to.Attributes[attributeTo]).Value != 808850000)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000); // driver
                                    }
                                }
                                else
                                    smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000); // driver
                            }
                            else if (subClaimType == 808850030 || subClaimType == 808850031) //passenger public transport  & passenger public transport no collision
                            {
                                if (from.Attributes.Contains("nal_busdrivercontact")) // bus details present
                                {
                                    if (to.Attributes.Contains(attributeTo))
                                    {
                                        if (((OptionSetValue)to.Attributes[attributeTo]).Value != 808850002)
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850002); // bus passenger
                                        }
                                    }
                                    else
                                        smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850002); // bus passenger
                                }
                            }
                            else if (subClaimType == 808850028) // passenger
                            {
                                if (to.Attributes.Contains(attributeTo))
                                {
                                    if (((OptionSetValue)to.Attributes[attributeTo]).Value != 808850001)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850001); //  passenger in a car van or lorry
                                    }
                                    else
                                        smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850001); //  passenger in a car van or lorry
                                }
                            }
                        }

                        if (attributeTo == "nal_ispassengerresponsibleforaccident")
                        {
                            var subClaimType = ((OptionSetValue)(from.Attributes[attributeFrom])).Value;
                            if (subClaimType == 808850028 || subClaimType == 808850030 || subClaimType == 808850031) // passenger and driver the defendant
                            {
                                if (from.Attributes.Contains("nal_rta_driveristhedefendant"))
                                {
                                    if ((bool)from.Attributes["nal_rta_driveristhedefendant"] == true)
                                    {
                                        if (to.Attributes.Contains(attributeTo))
                                        {
                                            if (((OptionSetValue)to.Attributes[attributeTo]).Value != 808850000)
                                            {
                                                smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000); //  passenger set to yes
                                            }
                                        }
                                        else
                                            smallClaimsUpdate.Attributes[attributeTo] = new OptionSetValue(808850000); //  passenger set to yes
                                    }

                                }
                            }
                        }

                        if (attributeTo == "nal_doesclientownthiscar")
                        {
                            var subClaimType = ((OptionSetValue)(from.Attributes[attributeFrom])).Value;
                            OptionSetValue newvalue = null;

                            if (subClaimType == 808850024 || subClaimType == 808850025 || subClaimType == 808850032) //driver, bus driver, taxi driver
                            {
                                if (from.Attributes.Contains("nal_rta_areyoutheownerofthevehicle"))
                                {
                                    if ((bool)from.Attributes["nal_rta_areyoutheownerofthevehicle"] == true)
                                    {
                                        newvalue = new OptionSetValue(808850000); //yes
                                    }
                                    else
                                    {
                                        newvalue = new OptionSetValue(808850001); //no
                                    }
                                }
                            }
                            else if (subClaimType == 808850028) //passenger
                            {
                                if (from.Attributes.Contains("nal_doesthecustomerownthevehicle"))
                                {
                                    if ((bool)from.Attributes["nal_doesthecustomerownthevehicle"] == true)
                                    {
                                        newvalue = new OptionSetValue(808850000); //yes
                                    }
                                    else
                                    {
                                        newvalue = new OptionSetValue(808850001); //no
                                    }
                                }
                            }
                            else
                            {
                                newvalue = new OptionSetValue(808850001); //no
                            }


                            //decide if we update
                            if (newvalue != null)
                            {
                                //decide if we update
                                if (to.Attributes.Contains(attributeTo))
                                {
                                    if (((OptionSetValue)to.Attributes[attributeTo]).Value != newvalue.Value)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = newvalue;
                                    }
                                }
                                else smallClaimsUpdate.Attributes[attributeTo] = newvalue;
                            }
                        }

                        //updated to here
                        if (attributeTo == "nal_registrationnumberofthevehicleclientwasin")
                        {
                            var subClaimType = ((OptionSetValue)(from.Attributes[attributeFrom])).Value;
                            var newValue = "";
                            if (subClaimType == 808850024 || subClaimType == 808850025 || subClaimType == 808850032) // all drivers
                            {
                                if (from.Attributes.Contains("nal_rta_vehicleregistrationnumber"))
                                {
                                    newValue = from.Attributes["nal_rta_vehicleregistrationnumber"].ToString();
                                }
                            }
                            else if (from.Attributes.Contains("nal_rta_driveristhedefendant"))  // if driver is defendant
                            {
                                if ((bool)from.Attributes["nal_rta_driveristhedefendant"] == true)
                                {
                                    if (from.Attributes.Contains("nal_defendantvehicleregistration"))
                                    {
                                        newValue = from.Attributes["nal_defendantvehicleregistration"].ToString();
                                    }
                                }
                            }
                            else if (from.Attributes.Contains("nal_rta_vehicleregistrationnumber")) // default catch
                            {
                                newValue = from.Attributes["nal_rta_vehicleregistrationnumber"].ToString();
                            }

                            if (to.Attributes.Contains(attributeTo))
                            {

                                if (to.Attributes[attributeTo].ToString() != newValue)
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                }
                            }
                            else
                                smallClaimsUpdate.Attributes[attributeTo] = newValue;
                        }

                        if (attributeTo == "nal_buscompanyname")
                        {
                            if (from.Attributes.Contains("nal_vehicleowner"))
                            {
                                if (to.Attributes.Contains(attributeTo))
                                {
                                    if (to.Attributes[attributeTo].ToString() != ((EntityReference)from.Attributes[attributeFrom]).Name)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name;
                                    }
                                }
                                else
                                    smallClaimsUpdate.Attributes[attributeTo] = ((EntityReference)from.Attributes[attributeFrom]).Name;
                            }
                        }

                        if (attributeTo == "nal_whoistheownerofthiscar")
                        {
                            var subClaimType = ((OptionSetValue)(from.Attributes[attributeFrom])).Value;
                            OptionSetValue newvalue = null;
                            if (subClaimType == 808850028) //passenger
                            {
                                //driver 
                                if (from.Attributes.Contains("nal_rta_doesthedriverownthevehicle"))
                                {
                                    if ((bool)from.Attributes["nal_rta_doesthedriverownthevehicle"] == true)
                                    {
                                        newvalue = new OptionSetValue(808850000); // driver
                                    }
                                }

                                //I am the owner
                                if (from.Attributes.Contains("nal_doesthecustomerownthevehicle"))
                                {
                                    if ((bool)from.Attributes["nal_doesthecustomerownthevehicle"] == true)
                                    {
                                        newvalue = new OptionSetValue(808850002); // I am the owner
                                    }
                                }

                                //someone else
                                if (from.Attributes.Contains("nal_doesthecustomerownthevehicle") && from.Attributes.Contains("nal_rta_doesthedriverownthevehicle"))
                                {
                                    if ((bool)from.Attributes["nal_doesthecustomerownthevehicle"] == false && (bool)from.Attributes["nal_rta_doesthedriverownthevehicle"] == false)
                                    {
                                        if (from.Attributes.Contains("nal_vehicleowner"))
                                        {
                                            newvalue = new OptionSetValue(808850001); // someone else
                                        }
                                        else
                                        {
                                            newvalue = new OptionSetValue(808850003); // unknown
                                        }
                                    }

                                }

                                if (newvalue != null)
                                {
                                    if (to.Attributes.Contains(attributeTo))
                                    {
                                        if (((OptionSetValue)to.Attributes[attributeTo]).Value != newvalue.Value)
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = newvalue;
                                        }
                                    }
                                    else
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = newvalue;
                                    }
                                }

                            }

                        }

                        if (attributeTo == "nal_reasonswhydefendantisatfault")
                        {
                            var allegationsQuery = new QueryExpression("pi_allegation")
                            {
                                ColumnSet = new ColumnSet(true),
                                Criteria = new FilterExpression { Conditions = { new ConditionExpression("nal_caseassessment", ConditionOperator.Equal, from.Id) } }
                            };
                            var allegationsString = "";
                            var allegations = service.RetrieveMultiple(allegationsQuery);
                            if (allegations != null && allegations.Entities.Count > 0)
                            {
                                foreach (var allegation in allegations.Entities)
                                {
                                    allegationsString += allegation.Attributes["pi_allegation"].ToString() + ";";
                                }
                                allegationsString = allegationsString.TrimEnd(';');
                            }

                            if (to.Attributes.Contains(attributeTo))
                            {
                                if (to.Attributes[attributeTo].ToString() != allegationsString)
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = allegationsString;
                                }
                            }
                            else
                                smallClaimsUpdate.Attributes[attributeTo] = allegationsString;
                        }

                        if (attributeTo == "nal_werethereanyothervehiclesinvolved")
                        {
                            if (from.Attributes.Contains("nal_werethereanywitnesses"))
                            {
                                //if (((OptionSetValue)from.Attributes["nal_werethereanywitnesses"]).Value == 808850000)
                                //{
                                var otherPartyQuery = new QueryExpression("pi_otherparty")
                                {
                                    ColumnSet = new ColumnSet(true),
                                    Criteria = new FilterExpression()
                                    {
                                        Conditions = {
                                                new ConditionExpression("nal_caseassessment",ConditionOperator.Equal,from.Id),
                                                new ConditionExpression("pi_isthispersonawitnessorsomeotherparty",ConditionOperator.Equal, 157430001) //other party
                                            }
                                    }
                                };

                                var otherParties = service.RetrieveMultiple(otherPartyQuery);
                                OptionSetValue newValue = null;

                                if (otherParties != null && otherParties.Entities.Count > 0)
                                {
                                    newValue = new OptionSetValue(808850000); //yes
                                }
                                else
                                {
                                    newValue = new OptionSetValue(808850001); //no
                                }

                                //decide if we update
                                if (newValue != null)
                                {
                                    if (!to.Attributes.Contains(attributeTo) && newValue.Value > 0)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                    }
                                    else if (((OptionSetValue)to.Attributes[attributeTo]).Value != newValue.Value)
                                    {
                                        if (to.Attributes[attributeTo] != newValue)
                                        {
                                            smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                        }
                                    }
                                    else if (newValue != null)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                    }
                                }

                                // }
                            }

                        }

                        if (attributeTo == "nal_howmanynightsdidyourclientstayinhospital")
                        {
                            if (from.Attributes.Contains("nal_wereyoukeptinthehospitalovernight"))
                            {
                                OptionSetValue newValue = null;

                                if ((bool)from.Attributes["nal_wereyoukeptinthehospitalovernight"] == true && from.Attributes.Contains("nal_ifyeshowmanynights"))
                                {
                                    var nightsInHospital = (int)from.Attributes["nal_ifyeshowmanynights"];
                                    if (nightsInHospital > 0 && nightsInHospital < 4)
                                    {
                                        newValue = new OptionSetValue(808850001); // 1-3 nights
                                    }
                                    else if (nightsInHospital > 3)
                                    {
                                        newValue = new OptionSetValue(808850002);
                                    }
                                }
                                else if ((bool)from.Attributes["nal_wereyoukeptinthehospitalovernight"] == false)
                                {
                                    newValue = new OptionSetValue(808850000); // no overnight stay
                                }

                                //decide if we update

                                if (!to.Attributes.Contains(attributeTo) && newValue.Value > 0)
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                }
                                else if (((OptionSetValue)to.Attributes[attributeTo]).Value != newValue.Value)
                                {
                                    if (to.Attributes[attributeTo] != newValue)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                    }
                                }
                                else if (newValue != null)
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                }

                            }
                        }

                        //nal_accidenttype
                        if (attributeTo == "nal_accidenttype")
                        {
                            // hit in rear -  808850000 
                            // pulling from side road 808850001
                            // changing lane 808850002
                            // concertina 808850003
                            // hit whilst parked 808850004
                            // hit by oncoming car 808850005
                            // other 808850006

                            OptionSetValue newValue = null;

                            if (from.Attributes.Contains("nal_rta_acccircvehhitbypartyfromsideroad"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircvehhitbypartyfromsideroad"] == true)
                                {
                                    newValue = new OptionSetValue(808850001);
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccircvehhitintherear"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircvehhitintherear"] == true)
                                {
                                    newValue = new OptionSetValue(808850000);
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccircvehhitwhilstparked"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircvehhitwhilstparked"] == true)
                                {
                                    newValue = new OptionSetValue(808850004);
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccircaccidentinacarpark"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircaccidentinacarpark"] == true)
                                {
                                    newValue = new OptionSetValue(808850006); //other
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccircaccidentonaroundabout"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircaccidentonaroundabout"] == true)
                                {
                                    newValue = new OptionSetValue(808850006); //other
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccircinvolvingvehicleschanginglanes"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircinvolvingvehicleschanginglanes"] == true)
                                {
                                    newValue = new OptionSetValue(808850002); //changing lanes
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccirchitbyoncomingcar"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccirchitbyoncomingcar"] == true)
                                {
                                    // if fast track set to other
                                    if (from.Attributes.Contains("nal_likelycasemanagementtrack"))
                                    {
                                        if (((OptionSetValue)(from.Attributes["nal_likelycasemanagementtrack"])).Value == 808850000)
                                        {
                                            newValue = new OptionSetValue(808850006); //other
                                        }
                                        else
                                        {
                                            newValue = new OptionSetValue(808850005); //oncoming car
                                        }
                                    }
                                    else
                                    {
                                        newValue = new OptionSetValue(808850005); //oncoming car
                                    }
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccircconcertinacollision"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircconcertinacollision"] == true)
                                {
                                    newValue = new OptionSetValue(808850003); //concertina
                                }
                            }
                            if (from.Attributes.Contains("nal_rta_acccircother"))
                            {
                                if ((bool)from.Attributes["nal_rta_acccircother"] == true)
                                {
                                    newValue = new OptionSetValue(808850006); //other
                                }
                            }

                            //decide if we update
                            if (to.Attributes.Contains(attributeTo) && newValue != null)
                            {
                                if (((OptionSetValue)to.Attributes[attributeTo]).Value != newValue.Value)
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                }
                            }
                            else if (newValue != null)
                            {
                                smallClaimsUpdate.Attributes[attributeTo] = newValue;
                            }

                        }

                        if (attributeTo == "nal_furthertreatmentrecommended")
                        {
                            OptionSetValue newValue = null;
                            if (from.Attributes.Contains(attributeFrom))
                            {

                                if (((OptionSetValue)(from.Attributes["nal_hasmedicalprofessionalrecommendedrehab"])).Value == 808850000) //yes
                                {
                                    newValue = new OptionSetValue(808850000); //  yes
                                }
                                else
                                {
                                    newValue = new OptionSetValue(808850001); // no
                                }

                                if (to.Attributes.Contains(attributeTo))
                                {
                                    if (((OptionSetValue)(from.Attributes[attributeTo])).Value != newValue.Value)
                                    {
                                        smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                    }
                                }
                                else
                                {
                                    smallClaimsUpdate.Attributes[attributeTo] = newValue;
                                }
                            }
                        }

                    }
                }
                catch (Exception ex) { Console.WriteLine(string.Format("Case Assessment Field[{0}] and Small Claims calculated Field[{1}] where not able to be mapped!\n : Error{2}", attributeFrom, attributeTo, ex.Message + ":" + ex.StackTrace)); }
            }

        }

        private string[][] getMapping() => new string[][]
        {
            new string[]{"nal_rta_busdriverid", //0
                         "nal_caseassessmentaccidentdescription",
                         "nal_accidentlocationaddress",
                         "nal_accidentlocation",
                         "nal_accidenttype",
                         "nal_askcuereference", //5
                         "nal_busdrivercontact",
                         "nal_rta_busroutenumber",
                         "nal_customercontact",
                         "nal_accidentdateandtime",
                         "nal_defendantdetails", //10
                         "nal_defendantvehiclecolour",
                         "nal_defendantvehiclemake",
                         "nal_defendantvehiclemodel",
                         "nal_defendantvehicleregistration",
                         "nal_haveyousoughtanymedicalattention",//15
                         "nal_didyouattendthegp",
                         "nal_rta_evidenceofbustravel",
                         "nal_otherdriverdetails",
                         "nal_occupation",
                         "nal_descriptionofthedefendant",//20
                         "nal_hasmedicalprofessionalrecommendedrehab",
                         "nal_sexofdefendant",
                         "nal_haveyousoughtanymedicalattention",
                         "nal_police_wastheincidentreportedtothepolice",
                         "nal_howwerethedefendantsdetailsobtained",//25
                         "nal_howmuchtimeoffhaveyouhaddays",
                         "nal_wereyoukeptinthehospitalovernight",
                         "nal_areyoustilloff",
                         "nal_rta_makeofvehicle",
                         "nal_rta_modelofvehicle", //30
                         "nal_police_nameofreportingofficer",
                         "nal_vehiclehirecompany",
                         "nal_rta_howmanypeoplewereinyourvehicle",
                         "nal_vehicleowner",
                         "nal_police_referencereferencenumber", //35
                         "nal_claimsubtype",
                         "nal_takenanytimeoffasaresultoftheinjury",
                         "nal_rta_wereyouwearingaseatbelt",
                         "nal_didyouattendthehospital",
                         "nal_werethereanywitnesses", //40
                         "nal_employmentstatus",
                         "nal_photosincidentdamagevehicles",
                         "nal_policeattendincident",
                         "nal_seekotherclinicalattendance",
                         "nal_whootherclinicalattenancesee", //45
                         "nal_norepairexplanationreason",
                         "nal_nohirecarexplanationreason",
                         "nal_worksource",
                         "nal_claimsubtype",
                         "nal_policestation", //50
                         "nal_claimsubtype",
                         "nal_claimsubtype",
                         "nal_vehicleowner",
                         "nal_claimsubtype",
                         "nal_aproxageofdefendant",
                         "nal_accidenttype",
                         "nal_werethereanywitnesses",
                         "nal_didyouattendthehospital",
                         "nal_rta_reqdefinsurerrepairinspectvehicle",
                         "nal_rta_doyourequireuseofanothervehicle",
                         "nal_doesthecustomerhavespecialdamages"},
            new string[]{"nal_busdriveridnumber", //0
                         "nal_accidentdescription",
                         "nal_accidentlocationaddress",
                         "nal_accidentlocationdescription",
                         "nal_accidenttype",
                         "nal_askcuepi", //5
                         "nal_busdriversname",
                         "nal_busroute",
                         "nal_claimantid",
                         "nal_datetimeofaccident",
                         "nal_defendantid", //10
                         "nal_defendantvehiclecolour",
                         "nal_defendantvehiclemake",
                         "nal_defendantvehiclemodel",
                         "nal_defendantvehiclevrm",
                         "nal_didclientseekanyotherclinicalattendance", //15
                         "nal_didyourclientseeagp",
                         "nal_doesclienthaveevidenceoftravel",
                         "nal_driverid",
                         "nal_employmentdescription",
                         "nal_enterclientsdescriptionofthedriver", //20
                         "nal_furthertreatmentrecommended",
                         "nal_genderofdriver",
                         "nal_hasclientsoughtmedicalattention",
                         "nal_hastheaccidentbeenreportedtothepolice",
                         "nal_howdidclientobtainthesedetails", //25
                         "nal_howmanydaysintotalwasyourclientoffwork",
                         "nal_howmanynightsdidyourclientstayinhospital",
                         "nal_isclientstilloffwork",
                         "nal_make",
                         "nal_model", //30
                         "nal_nameofreportingofficer",
                         "nal_nameofthehirecompany",
                         "nal_numberofpassangers",
                         "nal_ownerofthecarifnotclientid",
                         "nal_policereferencenumber", //35
                         "nal_registrationnumberofthevehicleclientwasin",
                         "nal_takentimeoffworkduetoinjury",
                         "nal_wasclientwearingaseatbelt",
                         "nal_washospitalattended",
                         "nal_werethereanywitnessesattheaccident", //40
                         "nal_employmentstatus",
                         "nal_istheredashcamorphotoevidence",
                         "nal_didthepoliceattendtheaccident",
                         "nal_didclientseekanyotherclinicalattendance",
                         "nal_whodidyourclientsee", //45
                         "nal_norepairexplanationreason",
                         "nal_nohirecarexplanationreason",
                         "nal_claimantreferralsource",
                         "nal_clientinvolvement",
                         "nal_nameandaddressofpolicestation", //50
                         "nal_ispassengerresponsibleforaccident",
                         "nal_doesclientownthiscar",
                         "nal_buscompanyname",
                         "nal_whoistheownerofthiscar",
                         "nal_enteranapproxageforthedriver",
                         "nal_reasonswhydefendantisatfault",
                         "nal_werethereanyothervehiclesinvolved",
                         "nal_enterthehospitalyourclientattended",
                         "nal_doesclientneedhelprepairingtheircar",
                         "nal_doesclientneedatemporaryvehicle",
                         "nal_doeswishtoclaimforanyotherlosses"}
        };

        private string[] getCalculatedFieldList() => new string[]
                         {   "nal_employmentdescription",
                             "nal_isregistrationknown",
                             "nal_enterthehospitalyourclientattended",
                             "nal_nameandaddressofpolicestation",
                             "nal_claimantreferralsource",
                             "nal_clientinvolvement",
                             "nal_busdriversname",
                             "nal_ispassengerresponsibleforaccident",
                             "nal_doesclientownthiscar",
                             "nal_registrationnumberofthevehicleclientwasin",
                             "nal_buscompanyname",
                             "nal_whoistheownerofthiscar",
                             "nal_reasonswhydefendantisatfault",
                             "nal_werethereanyothervehiclesinvolved",
                             "nal_howmanynightsdidyourclientstayinhospital",
                             "nal_accidenttype",
                             "nal_furthertreatmentrecommended"};


    }
}
