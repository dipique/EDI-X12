using System;
using System.Collections.Generic;
using System.Linq;

namespace EDI_Enrollment
{
    public class EDI_Enrollment_File: EDIFile
    {
        public string GroupID { get; set; } //optional. Only applicable if this file is for a single group
        public bool SeparateByGroup => !string.IsNullOrEmpty(GroupID);

        private string exportSuffix => $"{DataType.ToString().ToUpper()} {DateTime.UtcNow.ToString(EXPORT_DT_FMT)}.x12";
        public override string ExportFilename => string.IsNullOrEmpty(GroupID) ? exportSuffix : $"{GroupID}_{exportSuffix}";

        internal override string VRI_ID_CODE => "005010X220A1";
        internal override string TX_SET_ID => "834";


        /// <summary>
        /// Create the EDI Enrollment object
        /// </summary>
        /// <param name="controlNumberProvider"></param>
        /// <param name="dataType"></param>
        /// <param name="groupID">Only include the GroupID if the file is meant to be separated by group</param>
        public EDI_Enrollment_File(ControlNumberProvider controlNumberProvider, DataType dataType = DataType.Test, string groupID = null): base(controlNumberProvider, dataType)
        {
            GroupID = groupID;
        }

        /// <summary>
        /// OVERVIEW
        /// 
        /// Generating an arbitrary EDI X12 file without a library is a time intensive task. The task can, however,
        /// be greatly specified if you only have to export to one format AND don't have to import.
        /// 
        /// READING AN EDI X12 FILE
        /// 
        /// EDI files are text files. They use, for reasons I will never understand, both nested elements (like
        /// XML) and control loops. Because of this, it is impossible to know what an EDI file actually says unless
        /// you have its specification; EDI files are inherently ambiguous, because control loops don't have defined
        /// ending tags. It's almost as if the format was MEANT to be cryptic.
        /// 
        /// Although line breaks are (usually) excluded from the X12 specification, they CAN be included and they really
        /// improve the readability of a file. A single line of a these files might look like this:
        /// 
        /// GS*HC*1366952418*232238132*20180102*1733*5780518*X*005010X222A1~
        /// 
        /// This isn't nearly as cryptic as it looks. The "*" character is a delimiter, and the "~" character is an
        /// "end-of-segment" character. So even if your whole file is one unbroken line, you can just add line breaks
        /// after each "~".
        /// 
        /// FIELDS
        /// 
        /// Using the "*" delimiter, the line of data becomes a set of fields. The value in the first field ("GS" in this
        /// case) identifies the type of segment; in this case, it identifies the beginning of a group. All the remaining fields
        /// (their data types, definitions, and whether they're required) are specific to that type of segment, so an "ISA" segement
        /// will look completely different from a "GS" segment.
        /// 
        /// In a companion guide or file spec, each attribute will be named "GS101", "GS102", "GS103" and so on. "GS101" means "the
        /// first field after the "GS" identifier. "GS102" is the second field, and so forth. The interpreter of the file file
        /// COUNTS the field number to determine where it belongs. That means that even if GS101-GS106 aren't required and the only
        /// data you're submitting is in GS107, you still need to include the delimiters leading up to GS107, like this:
        /// 
        /// GS*******My data~
        /// 
        /// However, if GS108-GS112 are optional and you have no data, you can terminate the line early. Only empty fields BEFORE
        /// your data need to be represented by a delimiter.
        /// 
        /// CONTROL NUMBERS
        /// 
        /// The spec for a given segment may require a control number, which is an incrementing integer that identifies that segment. These
        /// are used almost exclusively for nesting segments--segments that use a "closing tag" segment--and must incremement across files,
        /// not merely within the file. So, if "0000001" is used in a file today, we cannot use "0000001" in a file tomorrow.
        /// 
        /// In this case, we use a ControlNumberProvider abstract class to provide control number functionality that can be tailored for a
        /// variety of different methods to store and increment these control numbers. The only thing to remember is to save the incremented
        /// control numbers at the end. This is annoying, but somewhat useful as it means a failed file generation can stop from incrementing
        /// the global control number set.
        /// 
        /// If the segment DOES require a control number, it will simple be one of the required fields for that segment.
        /// 
        /// FILE ASSEMBLY
        /// 
        /// For this implementation, the way we build the file is very simple. First, we create a List of strings, each of which will represent
        /// a "line" (a segment). We assemble the EDI file line by line, starting at the top. For each segment defined by the spec, we:
        /// 
        /// 1) Create a string array containing all the fields/data contained on that line. This data may come from a constant, the control
        ///    number object, a local variable, or data from the <paramref name="enrollees"/> input.
        ///    
        /// 2) Join the string array to a string with the field delimiter.
        /// 
        /// 3) Conclude the string with the end-of-segment identifier, "~".
        /// 
        /// 4) Add the full-assembled segment to the List of strings which represents the partially assembled EDI X12 file.
        /// 
        /// At the end, we save the control numbers (which have been incremented based on their use in the file), then return the entire file
        /// back as a string. In this case, we add newline characters ('/n') between each line, but that is not a requirement. The file can now
        /// be saved as a file; the correct filename to use is generated by the <see cref="ExportFilenameByGroup"/> read-only property according to the
        /// integrations specs.
        /// 
        /// USING ENUMS FOR READABILITY
        /// 
        /// Reading an EDI X12 file can be confusing, and the code can become similarly confusing. Enums are a useful way of reducing that
        /// confusion. For example, below, I use the enum <see cref="SegmentType"/> to indicate the segment. I'm using the DisplayName attribute
        /// for the enums to contact the constant value. For example, <see cref="SegmentType.GroupBegin_GS"/> has a DisplayName value of "GS", which
        /// is the expected segment identifier. The method <see cref="EDIVal{T}(T)"/> fetches that display value as a string that can be be included
        /// in the segment.
        /// 
        /// LOOPS
        /// 
        /// Control loops are replicated using logical loops. Simply use the appropriate iteration--foreach loops, in the below implementation--and
        /// generate the additional segments. This method is extremely flexible, but requires a strong understanding of the requirements of the file
        /// specs.
        /// 
        /// TIPS
        /// 
        /// 1) When first implementing a new file type, do so with a sample file open for reference. Using this sample file in conjunction with the
        ///    spec will help make the meaning of the spec more obvious.
        /// 2) The spec isn't always quite right. Ask questions if specific fields from the sample file don't seem to match the spec.
        /// 
        /// IMPORTING EDI FILES
        /// 
        /// This example does NOT help import EDI X12 files. However, it DOES suggest a method.
        /// 
        /// Using the approach below, creating a file definition would not be all that difficult, and one could then reverse the process of
        /// generating a file relatively easily.
        /// 
        /// </summary>
        /// <param name="enrollees"></param>
        /// <returns></returns>
        public string ToEDI(IEnumerable<Enrollee> enrollees, bool includeDependents = true, bool includeBeneficiaries = true)
        {
            //if these files are separated by group, make sure there aren't multiple groups represented in this set of enrollees
            if (SeparateByGroup)
            {
                //validate that we only have one group represented
                var tooManyGroups = enrollees.Any(e => e.EmploymentInfo.GroupID != GroupID);
                if (tooManyGroups) throw new Exception("Too many groups.");                
            }

            //set up the group loop; if there aren't multiple groups, it will only go through the loop once.
            var enrolleeSet = enrollees.GroupBy(e => e.EmploymentInfo.GroupID);

            EDILines = new List<string>();

            //get ISA header
            AddEDILine(SegmentType.InterchangeBegin_ISA,
                "00",
                new string(' ', 10),
                "00",
                new string(' ', 10),
                "ZZ",
                PARTNER_ID,
                "ZZ",
                RECEIVER_ID,
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
            AddEDILine(SegmentType.GroupBegin_GS,
                EDIVal(EDIFileType.Enrollment),
                PARTNER_ID,
                RECEIVER_ID,
                DateTime.UtcNow.ToString(DT_FMT_D8),
                DateTime.UtcNow.ToString("HHmmss"),
                ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.GroupBegin_GS)),
                "X",
                VRI_ID_CODE
            );

            //834 transaction starts here
            AddEDILine(SegmentType.TransactionSetBegin_ST,
                TX_SET_ID,
                ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.TransactionSetBegin_ST)),
                VRI_ID_CODE
            );

            foreach (var enrolleeGroup in enrolleeSet)
            {
                AddEDILine(SegmentType.TransactionBegin_BGN,
                    "00",
                    ControlNumbers.GetIncrementedStr(EDIVal(SegmentType.TransactionBegin_BGN)),
                    DateTime.UtcNow.ToString(DT_FMT_D8),
                    DateTime.UtcNow.ToString("HHmmss"),
                    "UT", //time code optional but it makes sense to send it anyway
                    "",
                    "",
                    "2"  //change; 4 is verify
                );

                AddEDILine(SegmentType.Info_REF,
                    "38",
                    enrolleeGroup.Key
                );

                //1000A
                AddEDILine(SegmentType.Party_N1,
                    "P5",
                    PARTNER_ID,
                    "FI",
                    SENDER_TIN
                );

                //1000B
                AddEDILine(SegmentType.Party_N1,
                    "IN",
                    RECEIVER_ID,
                    "FI",
                    RECEIVER_TIN
                );

                //2000
                foreach (var enrollee in enrolleeGroup.Where(e => e.ActiveOrRecentTerm))
                {

                    #region Add subscriber details

                    AddEDILine(SegmentType.Insured_INS,
                        EDIVal(IsSubscriber.Yes),
                        EDIVal(Relationship.Self),
                        EDIVal(enrollee.CancellationValuesToggle ? MaintenanceType.Cancellation : MaintenanceType.Add),
                        EDIVal(MaintenanceReason.NoReason),
                        "A",  //says "A" in documentation, but not used per Chris Faust
                        "",
                        "",
                        EDIVal(enrollee.CancellationValuesToggle ? EmploymentStatus.Terminated : EmploymentStatus.Active),
                        "",
                        EDIVal(IsBeneficiary.No)
                    );

                    AddEDILine(SegmentType.Info_REF,
                        "0F", //Subscriber number
                        enrollee.SubscriberID
                    );

                    AddEDILine(SegmentType.Info_REF,
                        "17",
                        EDIVal(enrollee.PayrollType)
                    );

                    AddEDILine(SegmentType.Info_REF,
                        "ZZ",
                        enrollee.AccountNumber
                    );

                    AddEDILine(SegmentType.Info_REF,
                        "23",
                        enrollee.RoutingNumber
                    );

                    AddEDILine(SegmentType.DateTime_DTP,
                        EDIVal(DateType.Received), //other option is effective date
                        "D8",
                        enrollee.DateSigned.ToString(DT_FMT_D8)
                    );

                    //2100A
                    AddEDILine(SegmentType.ResponsiblePerson_NM1,
                        EDIVal(ResponsiblePerson.InsuredOrSubscriber),
                        "1", //person
                        enrollee.Name.Last,
                        enrollee.Name.First,
                        enrollee.Name.MiddleInitial,
                        "", //suffix
                        "34", //ssn
                        enrollee.SSNNumeric //0 pad, numeric only
                    );

                    AddEDILine(SegmentType.ContactInfo_PER,
                        EDIVal(ContactInfoPersonType.InsuredParty),
                        "TE",
                        enrollee.Phone.ToNumeric(),
                        "AP",
                        enrollee.AltPhone.ToNumeric(),
                        "EM",
                        enrollee.Email
                    );


                    AddEDILine(SegmentType.AddressStreet_N3,
                        enrollee.Address.Street,
                        "" //second address line
                    );

                    AddEDILine(SegmentType.AddressCSZ_N4,
                        enrollee.Address.City,
                        enrollee.Address.State,
                        enrollee.Address.Zip
                    );

                    AddEDILine(SegmentType.Demographics_DMG,
                        "D8",
                        enrollee.DOB.ToString(DT_FMT_D8),
                        EDIVal(enrollee.Gender),
                        EDIVal(enrollee.MaritalStatus)
                    );

                    //2100C: Mailing address (if different)
                    if (enrollee.MailingAddress != null && !enrollee.MailingAddress.IsUnset)
                    {
                        AddEDILine(SegmentType.ResponsiblePerson_NM1,
                            EDIVal(ResponsiblePerson.MailingAddress),
                            "1" //person
                        );

                        AddEDILine(SegmentType.AddressStreet_N3,
                            enrollee.MailingAddress.Street,
                            "" //second address line
                        );

                        AddEDILine(SegmentType.AddressCSZ_N4,
                            enrollee.MailingAddress.City,
                            enrollee.MailingAddress.State,
                            enrollee.MailingAddress.Zip
                        );

                        AddEDILine(SegmentType.Demographics_DMG,
                            "D8",
                            enrollee.DOB.ToString(DT_FMT_D8),
                            EDIVal(enrollee.Gender),
                            EDIVal(enrollee.MaritalStatus)
                        );
                    }

                    //2300
                    AddEDILine(SegmentType.Coverage_HD,
                        EDIVal(enrollee.CancellationValuesToggle ? MaintenanceType.Cancellation : MaintenanceType.Add),
                        "",
                        "HLT",
                        enrollee.ElectedUnits.ToString().PadLeft(5, '0'),
                        EDIVal(enrollee.CoverageType)
                    );

                    AddEDILine(SegmentType.DateTime_DTP,
                        EDIVal(DateType.BenefitBegin),
                        "D8",
                        enrollee.EffectiveDate.ToString(DT_FMT_D8)
                    );

                    //include coverage termination only if there is a termination date
                    if (enrollee.IncludeCoverageEndDate())
                    {
                        AddEDILine(SegmentType.DateTime_DTP,
                            EDIVal(DateType.BenefitEnd),
                            "D8",
                            enrollee.TermDate.ToString(DT_FMT_D8)
                        );
                    }

                    AddEDILine(SegmentType.Info_REF,
                        "CE",
                        "LM"
                    );

                    //AddEDILine( //not used, in the specs inaccurately
                    //    EDIVal(SegmentType.Info),
                    //    "E8",
                    //    EDIVal(enrollee.PayrollType)
                    //});

                    #endregion

                    if (includeDependents)
                    {
                        #region Add dependent details

                        foreach (var dependent in enrollee.Dependents)
                        {
                            AddEDILine(SegmentType.Insured_INS,
                                EDIVal(IsSubscriber.No),
                                EDIVal(dependent.Relationship),
                                EDIVal(enrollee.CancellationValuesToggle ? MaintenanceType.Cancellation : MaintenanceType.Add),
                                EDIVal(MaintenanceReason.NoReason),
                                "A",  //says "A" in documentation, but not used per Chris Faust
                                "",
                                "",
                                EDIVal(enrollee.CancellationValuesToggle ? EmploymentStatus.Terminated : EmploymentStatus.Active)
                            );

                            AddEDILine(SegmentType.Info_REF,
                                "0F", //Subscriber number
                                enrollee.SubscriberID
                            );

                            AddEDILine(SegmentType.DateTime_DTP,
                                EDIVal(DateType.Received), //other option is effective date
                                "D8",
                                enrollee.DateSigned.ToString(DT_FMT_D8)
                            );

                            //2100A
                            AddEDILine(SegmentType.ResponsiblePerson_NM1,
                                "IL", //this will never change
                                "1", //person
                                dependent.Name.Last,
                                dependent.Name.First,
                                dependent.Name.MiddleInitial,
                                "", //suffix
                                "34", //ssn
                                dependent.SSNNumeric //0 pad, numeric only
                            );

                            AddEDILine(SegmentType.Demographics_DMG,
                                "D8",
                                dependent.DOB.ToString(DT_FMT_D8),
                                EDIVal(dependent.Gender)
                            );

                            //2300: Coverage loop
                            AddEDILine(SegmentType.Coverage_HD,
                                EDIVal(enrollee.CancellationValuesToggle ? MaintenanceType.Cancellation : MaintenanceType.Add),
                                "",
                                "HLT", //indicates that next item is elected units
                                enrollee.ElectedUnits.ToString().PadLeft(5, '0'),
                                EDIVal(enrollee.CoverageType)
                            );

                            AddEDILine(SegmentType.DateTime_DTP,
                                EDIVal(DateType.BenefitBegin),
                                "D8",
                                enrollee.EffectiveDate.ToString(DT_FMT_D8)
                            );

                            if (enrollee.Termed())
                            {
                                AddEDILine(SegmentType.DateTime_DTP,
                                    EDIVal(DateType.BenefitEnd),
                                    "D8",
                                    enrollee.TermDate.ToString(DT_FMT_D8)
                                );
                            }

                            AddEDILine(SegmentType.Info_REF,
                                "CE",
                                "LM"
                            );

                            AddEDILine(SegmentType.Info_REF,
                                "E8",
                                EDIVal(enrollee.PayrollType)
                            );
                        }

                        #endregion
                    }

                    if (includeBeneficiaries)
                    {
                        #region Add beneficiary details

                        //2000: Beneficiaries loop
                        foreach (var beneficiary in enrollee.Beneficiaries)
                        {
                            AddEDILine(SegmentType.Insured_INS,
                                EDIVal(IsSubscriber.No),
                                EDIVal(Relationship.Other),
                                EDIVal(enrollee.CancellationValuesToggle ? MaintenanceType.Cancellation : MaintenanceType.Add),
                                EDIVal(MaintenanceReason.NoReason),
                                "A",  //says "A" in documentation, but not used per Chris Faust
                                "",
                                "",
                                EDIVal(enrollee.CancellationValuesToggle ? EmploymentStatus.Terminated : EmploymentStatus.Active),
                                "",
                                EDIVal(IsBeneficiary.Yes),
                                "",   //death date qualifier
                                "",   //death date
                                "",
                                "",
                                "",
                                "",
                                beneficiary.Percent.ToString()
                            );

                            AddEDILine(SegmentType.Info_REF,
                                "0F", //Subscriber number
                                enrollee.SubscriberID
                            );

                            //2100A (beneficiary)
                            AddEDILine(SegmentType.ResponsiblePerson_NM1,
                                EDIVal(ResponsiblePerson.InsuredOrSubscriber),
                                "1", //person
                                beneficiary.Name.Last,
                                beneficiary.Name.First,
                                beneficiary.Name.MiddleInitial
                            );
                        }

                        #endregion
                    }
                }
            }

            AddEDILine(SegmentType.TransactionSetEnd_SE,
                enrollees.Count().ToString(),
                ControlNumbers.GetCurrentStr(EDIVal(SegmentType.TransactionSetBegin_ST))
            );

            AddEDILine(SegmentType.GroupEnd_GE,
                "1",
                ControlNumbers.GetCurrentStr(EDIVal(SegmentType.GroupBegin_GS))
            );

            AddEDILine(SegmentType.InterchangeEnd_IEA,
                "1",
                ControlNumbers.GetCurrentStr(EDIVal(SegmentType.InterchangeBegin_ISA), 9)
            );

            //save control numbers
            ControlNumbers.SaveControlNumbers();

            return string.Join("\n", EDILines);
        }
    }
}
