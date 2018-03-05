using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDI_Enrollment
{
    public abstract class Person
    {
        public PersonName Name { get; set; } = new PersonName();
        public Gender Gender { get; set; }
        public DateTime DOB { get; set; }
        public string SSN { get; set; } = string.Empty;

        //contact info
        public string Phone { get; set; } = string.Empty;
        public string AltPhone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Address Address { get; set; } = new Address();

        public string SSNNumeric => SSN == null ? string.Empty : new string(SSN.Where(c => char.IsDigit(c)).ToArray());

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

    public class Dependent: Person
    {
        public Relationship Relationship { get; set; } = Relationship.Other;
    }

    public class Patient: Dependent
    {
        public Patient()
        {
            Relationship = Relationship.Self; //default is self rather than Other.
        }
    }
}
