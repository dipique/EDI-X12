using System;
using System.Collections.Generic;
using System.Linq;

namespace EDI_Enrollment
{
    public class EDI_Claim_File: EDIFile
    {
        public override string ExportFilename => $"{DataType.ToString().ToUpper()}_{DateTime.UtcNow.ToString(EXPORT_DT_FMT)}.x12";
        internal override string VRI_ID_CODE => "005010X222A1"; //should really be "005010X12"
        internal override string TX_SET_ID => "837";

        internal string Taxonomy_Code => "193200000X";

        /// <summary>
        /// Create the EDI Claim File object
        /// </summary>
        /// <param name="controlNumberProvider"></param>
        /// <param name="dataType"></param>
        public EDI_Claim_File(ControlNumberProvider controlNumberProvider, DataType dataType = DataType.Test): base(controlNumberProvider, dataType) { }

        public string ToEDI(IEnumerable<Claim> claims, Submitter submitter) //todo: group by submitter
        {
            EDILines = new List<string>();
            int HLLevel = 1;

            //get ISA header
            AddEDILine(EDIVal(SegmentType.InterchangeBegin_ISA),
                "00",
                new string(' ', 10),
                "00",
                new string(' ', 10),
                "ZZ",
                SENDER_TIN.PadToLength(15),
                "ZZ",
                RECEIVER_TIN.PadToLength(15),
                DateTime.UtcNow.ToString("yyMMdd"),
                DateTime.UtcNow.ToString("HHmm"),
                REPETITION_DELIM,
                "00501",
                ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.InterchangeBegin_ISA), 9),
                "0", //ack not required
                EDIVal(DataType),
                USAGE_DELIM
            );

            //group header
            AddEDILine(EDIVal(SegmentType.GroupBegin_GS),
                EDIVal(EDIFileType.HealthcareClaim),
                PARTNER_ID,
                RECEIVER_ID,
                DateTime.UtcNow.ToString(DT_FMT_D8),
                DateTime.UtcNow.ToString("HHmm"),
                ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.GroupBegin_GS)),
                "X",
                VRI_ID_CODE
            );

            //837 transaction starts here
            AddEDILine(EDIVal(SegmentType.TransactionSetBegin_ST),
                TX_SET_ID,
                ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.TransactionSetBegin_ST)).PadToLength(4,'0'),
                VRI_ID_CODE
            );

            AddEDILine(EDIVal(SegmentType.HLTxBegin_BHT),
                "0019", //structure code
                "00", //tx purpose: original, other option: 18
                ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.HLTxBegin_BHT)),
                DateTime.UtcNow.ToString(DT_FMT_D8),
                DateTime.UtcNow.ToString("HHmm"),
                "CH" //chargeable (other option: RP)
            );

            //1000A: submitter
            AddEDILine(EDIVal(SegmentType.ResponsiblePerson_NM1),
                EDIVal(ResponsiblePersonType.Submitter),
                EDIVal(submitter.Type),
                submitter.SubmitterName,
                submitter.FirstName, //empty if organization
                submitter.Name.MiddleInitial, //empty if organization
                "", //NM106 - prefix
                "", //suffix
                "46", //ID code qualifier: ETIN
                SENDER_TIN
            );

            //submitter contact info
            AddEDILine(EDIVal(SegmentType.ContactInfo_PER),
                EDIVal(ContactInfoPersonType.InformationContact),
                submitter.SubmitterName,
                EDIVal(ContactInfoType.Telephone),
                submitter.Phone.ToNumeric(),
                EDIVal(ContactInfoType.Email),
                submitter.Email
            );

            //1000B: receiver (Insurer/TPA)
            AddEDILine(EDIVal(SegmentType.ResponsiblePerson_NM1),
                EDIVal(ResponsiblePersonType.Receiver),
                EDIVal(EntityType.NonPerson),
                RECEIVER_ID,
                "", "", "", "", //NM104-NM107
                EDIVal(IDType.ETIN),
                RECEIVER_TIN
            );

            foreach (var claim in claims)
            {

                //2000A: billing provider Loop
                AddEDILine(EDIVal(SegmentType.HierarchicalLevel_HL),
                    (HLLevel).ToString(),
                    "", //HL02
                    EDIVal(HierarchicalLevelCode.InformationSource),
                    EDIVal(HierarchicalChildCode.HasChild)
                );

                AddEDILine(EDIVal(SegmentType.BillingProviderTaxonomy_PRV),
                    "BI",
                    "PXC",
                    Taxonomy_Code
                );

                //2010AA: Billing Provider
                AddEDILine(EDIVal(SegmentType.ResponsiblePerson_NM1),
                    EDIVal(ResponsiblePersonType.BillingProvider),
                    EDIVal(submitter.Type),
                    submitter.SubmitterName,
                    submitter.FirstName, //empty if organization
                    submitter.Name.MiddleInitial, //empty if organization
                    "", //NM106
                    "", //suffix
                    EDIVal(IDType.NationalProviderID),
                    submitter.NPINumber
                );

                AddEDILine(EDIVal(SegmentType.AddressStreet_N3),
                    submitter.Address.Street,
                    "" //second address line
                );

                AddEDILine(EDIVal(SegmentType.AddressCSZ_N4),
                    submitter.Address.City,
                    submitter.Address.State,
                    submitter.Address.Zip
                );

                AddEDILine(EDIVal(SegmentType.Info_REF),
                    EDIVal(IDType.EIN),
                    submitter.EIN
                );

                //2000B: Subscriber loop
                AddEDILine(EDIVal(SegmentType.HierarchicalLevel_HL),
                    (HLLevel + 1).ToString(),
                    (HLLevel).ToString(),
                    EDIVal(HierarchicalLevelCode.Subscriber),
                    EDIVal(claim.ChildCode)
                );
                HLLevel += 2;

                AddEDILine(EDIVal(SegmentType.Subscriber_SBR),
                    EDIVal(ResponsibilitySequence.PrimaryPayer),
                    EDIVal(claim.Patient.Relationship),
                    claim.EmploymentInfo.GroupID,
                    claim.EmploymentInfo.EmployerName,
                    "", //insurance type code
                    "", "", "", //SBR06-SBR08
                    "15" //type of claim
                );

                //2010BA
                AddEDILine(EDIVal(SegmentType.ResponsiblePerson_NM1),
                    EDIVal(ResponsiblePerson.InsuredOrSubscriber),
                    EDIVal(EntityType.Person),
                    claim.LastName,
                    claim.FirstName,
                    claim.Name.MiddleInitial,
                    "", "", //prefix & suffix: NM106-NM107
                    EDIVal(IDType.MemberID),
                    claim.SubscriberID
                );

                AddEDILine(EDIVal(SegmentType.AddressStreet_N3),
                    claim.Address.Street,
                    "" //second address line
                );

                AddEDILine(EDIVal(SegmentType.AddressCSZ_N4),
                    claim.Address.City,
                    claim.Address.State,
                    claim.Address.Zip
                );

                AddEDILine(EDIVal(SegmentType.Demographics_DMG),
                    "D8",
                    claim.DOB.ToString(DT_FMT_D8),
                    EDIVal(claim.Gender),
                    EDIVal(claim.MaritalStatus)
                );

                AddEDILine(EDIVal(SegmentType.ResponsiblePerson_NM1),
                    EDIVal(ResponsiblePersonType.Payer),
                    EDIVal(EntityType.NonPerson),
                    RECEIVER_ID,
                    "", "", "", "", //NM104-NM107
                    EDIVal(IDType.PlanID),
                    RECEIVER_TIN //I'm at a loss as to how this is the PlanID
                );

                //2300: Claim Information
                AddEDILine(EDIVal(SegmentType.ClaimInfo_CLM),
                    claim.ClaimNumber,
                    claim.ClaimAmount,
                    "", "", //CLM03 & CLM04
                    claim.Facility,
                    EDIVal(ResponseCode.No), //no physical signature
                    "C", //medicare assignment code
                    EDIVal(ResponseCode.No), //response code to... I'm not actually sure.
                    "I", //release of information, other option "Y"
                    "P" //patient signature on file
                );

                AddEDILine(EDIVal(SegmentType.Diagnosis_HI),
                    $"{EDIVal(CodeList.ICD10)}{USAGE_DELIM}{claim.ICDCode}"
                );

                foreach (var service in claim.Services)
                {
                    //2400: Service Line
                    AddEDILine(EDIVal(SegmentType.ServiceLine_LX),
                        ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.ServiceLine_LX))
                    );

                    AddEDILine(EDIVal(SegmentType.ProfessionalService_SV1),
                        $"HC{USAGE_DELIM}{service.ProcedureCode}",
                        service.Amount.ToString(),
                        "UN", //Units
                        service.Units.ToString(),
                        "",
                        "",
                        "1", //diagnosis code pointer
                        "", "", "", "", "", "", "", "", "", //SV108-SV116
                        service.ServiceID
                    );

                    AddEDILine(EDIVal(SegmentType.DateTime_DTP),
                        EDIVal(DateType.DateOfService),
                        "D8",
                        service.DateOfService.ToString(DT_FMT_D8)
                    );
                }
            }

            var segmentCount = EDILines.Count() - 1; //initial ISA segment doesn't count
            AddEDILine(EDIVal(SegmentType.TransactionSetEnd_SE),
                segmentCount.ToString(),
                ControlNumbers.GetCurrentStr(EDIVal(SegmentType.TransactionSetBegin_ST)).PadToLength(4, '0')
            );

            AddEDILine(EDIVal(SegmentType.GroupEnd_GE),
                "1",
                ControlNumbers.GetCurrentStr(EDIVal(SegmentType.GroupBegin_GS))
            );

            AddEDILine(EDIVal(SegmentType.InterchangeEnd_IEA),
                "1",
                ControlNumbers.GetCurrentStr(EDIVal(SegmentType.InterchangeBegin_ISA), 9)
            );

            //save control numbers
            ControlNumbers.SaveControlNumbers();

            return string.Join(JoinString, EDILines);
        }
    }
}
