using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

/// <summary>
/// These are all structs for all intents and purposes (except for PersonName), but linq expressions don't play well with structs
/// </summary>
namespace EDI_Enrollment
{
    public class Address
    {
        [MaxLength(40)] public string Street { get; set; } = string.Empty;
        [MaxLength(29)] public string City { get; set; } = string.Empty;
        [MaxLength(2)] public string State { get; set; } = string.Empty;
        [MaxLength(9)] public string Zip { get; set; } = string.Empty;

        public override string ToString() => $"{Street}, {City}, {State} {Zip}";
        public bool IsUnset => new Address().ToString() == ToString();
    }

    public class PersonName
    {
        [Required]
        [MaxLength(17)] public string First { get; set; } = string.Empty;

        private string middleInitial = string.Empty;
        [MaxLength(1)] public string MiddleInitial
        {
            get => middleInitial.Length == 0 ? string.Empty : middleInitial.Substring(0, 1);
            set => middleInitial = value.Trim();
        }

        [Required]
        [MaxLength(20)] public string Last { get; set; } = string.Empty;

        public override string ToString() => 
            string.IsNullOrWhiteSpace(MiddleInitial) ? $"{First.Trim()} {Last.Trim()}"
                                                     : $"{First.Trim()} {MiddleInitial} {Last.Trim()}";
    }

    public class EmploymentInfo
    {
        public string EmployerName { get; set; } = string.Empty;

        [Required]
        public string GroupID { get; set; } = string.Empty;
        public EmployeeStatus EmployeeStatus { get; set; }
    }

    public class Beneficiary
    {
        [Required]
        public PersonName Name { get; set; } = new PersonName();
        public Relationship Relationship { get; set; } = Relationship.Other;
        public int Percent { get; set; }

        public string FirstName
        {
            get => Name.First;
            set => Name.First = value;
        }

        public string LastName
        {
            get => Name.Last;
            set => Name.Last = value;
        }
    }

    public class Submitter: Person
    {
        public EntityType Type { get; set; }
        public string OrganizationName { get; set; } = string.Empty; //if type is non-person
        public string NPINumber { get; set; } = string.Empty;
        public string EIN { get; set; } = string.Empty;

        public string SubmitterName => Type == EntityType.Person ? LastName : OrganizationName;
    }

    public class Service
    {
        public string ProcedureCode { get; set; } = string.Empty;
        public string ServiceID { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 1; //defaults to $1 because these are expected to be $0
        public int Units { get; set; } = 1;
        public DateTime DateOfService { get; set; }
    }
}
