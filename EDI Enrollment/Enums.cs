using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Some of these enums are purely for convenience, but most actually have their EDI representations
/// embedded in the "Display" property. This can be nice because it acts as a "Dictionary" for what
/// certain arbitrary values mean.
/// </summary>
namespace EDI_Enrollment
{
    public enum EnrollmentType
    {
        NewCoverage,
        Modification,
        OpenEnrollment,
        SpecialEnrollment
    }

    public enum EDIFileType
    {
        [Display(Name = "HC")]
        HealthcareClaim,

        [Display(Name = "BE")]
        Enrollment
    }

    public enum Gender
    {
        [Display(Name = "M")]
        Male,

        [Display(Name = "F")]
        Female,

        [Display(Name = "U")]
        Unknown
    }

    public enum ContactType
    {
        Home,
        Work
    }
    
    public enum ContactMethod
    {
        Email,
        Phone
    }

    public enum EligibilityStatus
    {
        Salary,
        Hourly
    }

    public enum EmployeeStatus
    {
        W2,
        Owner
    }

    public enum EmploymentStatus
    {
        [Display(Name = "AC")]
        Active,

        [Display(Name = "TE")]
        Terminated
    }

    public enum ResponsiblePerson
    {
        [Display(Name = "IL")]
        InsuredOrSubscriber,

        [Display(Name = "74")]
        CorrectedSubscriber, //not used for new enrollments

        [Display(Name = "QD")]
        ResponsiblePerson,

        [Display(Name = "QD")] //same thing as ResponsiblePerson, but makes the code easier to read
        Beneficiary,

        [Display(Name = "31")]
        MailingAddress //hey, I didn't design this file
    }

    public enum RelationshipStatus
    {
        Single,
        Married,
        Legally_Separated,
        Common_Law,
        Divorced,
        Civil_Union,
        Domestic_Partnership,
        Widow_Or_Widower
    }

    public enum MaritalStatus
    {
        [Display(Name = "I")]
        Single,

        [Display(Name = "M")]
        Married
    }

    public enum Relationship
    {
        [Display(Name = "18")]
        Self,

        [Display(Name = "01")]
        Spouse,

        [Display(Name = "53")]
        LifePartner,

        [Display(Name = "19")]
        Child,

        [Display(Name = "G8")]
        Other //related
    }

    public enum CoverageType
    {
        [Display(Name = "EMP")]
        EmployeeOnly,

        [Display(Name = "ESP")]
        EmployeePlusSpouse,

        [Display(Name = "ECH")]
        EmployeePlusChildren,

        [Display(Name = "FAM")]
        EmployeePlusFamily
    }

    public enum PayrollType
    {
        [Display(Name = "00001")]
        Weekly,

        [Display(Name = "00002")]
        Biweekly,

        [Display(Name = "00003")]
        SemiMonthly,

        [Display(Name = "00004")]
        Monthly
    }

    public enum DataType
    {
        [Display(Name = "T")]
        Test,

        [Display(Name = "P")]
        Prod
    }

    public enum MaintenanceType
    {
        [Display(Name = "001")]
        Change,

        [Display(Name = "021")]
        Add,

        [Display(Name = "024")]
        Cancellation
    }

    public enum MaintenanceReason
    {
        [Display(Name = "01")]
        Divorce,

        [Display(Name = "02")]
        Birth,

        [Display(Name = "03")]
        Death,

        [Display(Name = "05")]
        Adoption,

        [Display(Name = "AL")]
        NoReason
    }

    public enum DateType
    {
        [Display(Name = "050")]
        Received,

        [Display(Name = "336")]
        EffectiveDate,

        [Display(Name = "348")]
        BenefitBegin,

        [Display(Name = "349")]
        BenefitEnd,

        [Display(Name = "435")]
        Admission,

        [Display(Name = "096")]
        Discharge,

        [Display(Name = "472")]
        DateOfService
    }

    public enum IsSubscriber
    {
        [Display(Name = "Y")]
        Yes,

        [Display(Name = "N")]
        No
    }

    public enum IsBeneficiary
    {
        [Display(Name = "Y")]
        Yes,

        [Display(Name = "N")]
        No
    }

    public enum SegmentType
    {
        [Display(Name = "ISA")]
        InterchangeBegin_ISA,

        [Display(Name = "IEA")]
        InterchangeEnd_IEA,

        [Display(Name = "GS")]
        GroupBegin_GS,

        [Display(Name = "GE")]
        GroupEnd_GE,

        [Display(Name = "ST")]
        TransactionSetBegin_ST,

        [Display(Name = "BGN")]
        TransactionBegin_BGN,

        [Display(Name = "SE")]
        TransactionSetEnd_SE,

        [Display(Name = "PER")]
        ContactInfo_PER,

        [Display(Name = "INS")]
        Insured_INS,

        [Display(Name = "DMG")]
        Demographics_DMG,

        [Display(Name = "REF")]
        Info_REF,

        [Display(Name = "N1")]
        Party_N1,

        [Display(Name = "N3")]
        AddressStreet_N3,

        [Display(Name = "N4")]
        AddressCSZ_N4,

        [Display(Name = "NM1")]
        ResponsiblePerson_NM1,

        [Display(Name = "DTP")]
        DateTime_DTP,

        [Display(Name = "HD")]
        Coverage_HD,

        [Display(Name = "BHT")]
        HLTxBegin_BHT,

        [Display(Name = "HL")]
        HierarchicalLevel_HL,

        [Display(Name = "PRV")]
        BillingProviderTaxonomy_PRV,

        [Display(Name = "SBR")]
        Subscriber_SBR,

        [Display(Name = "CLM")]
        ClaimInfo_CLM,

        [Display(Name = "HI")]
        Diagnosis_HI,

        [Display(Name = "LX")]
        ServiceLine_LX,

        [Display(Name = "SV1")]
        ProfessionalService_SV1
    }

    public enum EntityType
    {
        [Display(Name = "1")]
        Person,

        [Display(Name = "2")]
        NonPerson //organization
    }

    public enum ContactInfoPersonType
    {
        [Display(Name = "IC")]
        InformationContact,

        [Display(Name = "IP")]
        InsuredParty
    }

    public enum ContactInfoType
    {
        [Display(Name = "TE")]
        Telephone,

        [Display(Name = "EM")]
        Email
    }

    public enum ResponsiblePersonType
    {
        [Display(Name = "40")]
        Receiver,

        [Display(Name = "41")]
        Submitter,

        [Display(Name = "85")]
        BillingProvider,

        [Display(Name = "PR")]
        Payer
    }

    public enum IDType
    {
        [Display(Name = "XX")]
        NationalProviderID,

        [Display(Name = "46")]
        ETIN,

        [Display(Name = "EI")]
        EIN,

        [Display(Name = "1G")]
        Misc_Other,

        [Display(Name = "MI")]
        MemberID,

        [Display(Name = "34")]
        SSN,

        [Display(Name = "34")]
        PlanID
    }

    public enum HierarchicalLevelCode
    {
        [Display(Name = "22")]
        Subscriber,

        [Display(Name = "20")]
        InformationSource
    }

    public enum HierarchicalChildCode
    {
        [Display(Name = "0")]
        NoChild,

        [Display(Name = "1")]
        HasChild
    }

    public enum ResponsibilitySequence
    {
        [Display(Name = "P")]
        PrimaryPayer,

        [Display(Name = "S")]
        SecondaryPayer
    }

    public enum ResponseCode
    {
        [Display(Name = "Y")]
        Yes,

        [Display(Name = "N")]
        No
    }

    public enum CodeList
    {
        [Display(Name = "BK")]
        ICD9,

        [Display(Name = "ABK")]
        ICD10
    }
}
