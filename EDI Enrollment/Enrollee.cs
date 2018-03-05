using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EDI_Enrollment
{
    public class Enrollee : Person
    {
        //number of days back to look for recent terms. This is important because,
        //in my implementation, 834 files were comprehensive, which means terminated
        //enrollees needs to be send for a certain number of days after termination
        //so they could be updated.
        private const int RECENT_TERM_DAYS = 30; 

        //coverage information
        public EnrollmentType EnrollmentType { get; set; } = EnrollmentType.NewCoverage;

        //employer information
        public EmploymentInfo EmploymentInfo { get; set; }
        public string GroupID => EmploymentInfo?.GroupID ?? string.Empty; //purely for ease of grouping

        //employee information
        [MaxLength(10)]
        public string SubscriberID { get; set; } = string.Empty;
        public int Age => (int)Math.Floor((DateSigned - DOB).Days / 365m);
        public Address MailingAddress { get; set; } //if different
        public RelationshipStatus RelationshipStatus { get; set; }
        public MaritalStatus MaritalStatus => RelationshipStatus == RelationshipStatus.Married ? MaritalStatus.Married : MaritalStatus.Single;

        //bank information
        //[Required] public string BankName { get; set; } //not in the EDI file anyway
        [MaxLength(20)] [Required] public string AccountNumber { get; set; } = string.Empty;
        [MaxLength(9)] [Required] public string RoutingNumber { get; set; } = string.Empty;
        public PayrollType PayrollType { get; set; }

        //coverage selection
        public CoverageType CoverageType { get; set; }
        public int ElectedUnits { get; set; }
        public List<Dependent> Dependents { get; set; } = new List<Dependent>();
        public List<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();


        //dates
        public DateTime EffectiveDate { get; set; }
        public DateTime DateSigned { get; set; } = DateTime.Today; //date enrollment app was signed
        public DateTime TermDate { get; set; } //termination date

        /// <summary>
        /// Returns whether the enrollee was termed within the last n days (or at all, if no number of days is provided).
        /// </summary>
        /// <param name="days">Optional. Number of days back to look for a termination. If not provided, will return whether the enrollee is termed in the past.</param>
        /// <returns></returns>
        public bool Termed(int days = -1)
        {
            //if they weren't termed return false
            if (TermDate == default(DateTime)) return false;

            //if there's no term days set, we can just return true if the term was in the past
            if (days == -1) return DateTime.Today >= TermDate;

            //return whether a termination happened in the date range provided (AND happened in the past, not in the future)
            return DateTime.Today >= TermDate &&                   //the term happened in the past, it's not a scheduled future term 
                   DateTime.Today <= TermDate.Date.AddDays(days);  //the term happened within the timeframe described
        }

        public bool RecentTerm => Termed(RECENT_TERM_DAYS);
        public bool ActiveAsOf(DateTime asOf) => TermDate == default(DateTime) || (!PreEnrollmentCancellation && TermDate.Date > DateTime.Today);
        public bool PreEnrollmentCancellation => EffectiveDate == TermDate;

        /// <summary>
        /// This is the population of individuals that should be reported to the insurance company on a daily basis
        /// (based on term date). This includes active enrollees, enrollees terminated within the last 30 days, and
        /// enrollees that will be terminated at the end of the current month, as well as enrollees that WILL be 
        /// active in the future.
        /// </summary>
        public bool ActiveOrRecentTerm => ActiveAsOf(DateTime.Today) || RecentTerm || EffectiveDate > DateTime.Today;

        /// <summary>
        /// Types of term dates:
        ///   -Null/unset - Do not include
        ///   -Same as effective date (pre-enrollment cancellation) - Include
        ///   -Equal to or prior to today - Include
        ///   -After today - Do not include
        /// </summary>
        public bool IncludeCoverageEndDate(DateTime asOf = default(DateTime))
        {
            if (asOf == default(DateTime)) asOf = DateTime.Today;

            if (TermDate == default(DateTime)) return false;

            if (PreEnrollmentCancellation) return true;
            if (TermDate <= DateTime.Today) return true;

            return false;
        }

        //we use the cancellation values in the enrollment file for past terminations and pre-enrollment terminations
        public bool CancellationValuesToggle => Termed() || PreEnrollmentCancellation;
    }
}
