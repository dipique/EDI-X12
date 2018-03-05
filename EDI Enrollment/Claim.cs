using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDI_Enrollment
{
    public class Claim : Enrollee
    {
        public Submitter Submitter { get; set; } = new Submitter();  //need name/organization, phone
        public Patient Patient { get; set; } = new Patient();
        public string ClaimNumber { get; set; } = string.Empty; //assigned by the HMP
        public string ClaimAmount => Services.Sum(s => s.Amount).ToString();
        public string Facility { get; set; } = "12:B:1";
        public string ICDCode { get; set; }

        public List<Service> Services { get; set; } = new List<Service>();

        public HierarchicalChildCode ChildCode => 
            Patient.Relationship == Relationship.Self ? HierarchicalChildCode.NoChild 
                                                      : HierarchicalChildCode.HasChild;
    }
}
