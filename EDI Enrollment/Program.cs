using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EDI_Enrollment
{
    class Program
    {
        public const string IMPORT_FILENAME = "import.dat";
        public static DateTime DefaultEffectiveDate = new DateTime(2018, 2, 1);

        /// <summary>
        /// To use this EDI generator, enter data into the Excel sample file (import templates\Enrollee/Claim Import - Sample.xlsx), then copy and paste 
        /// ALL the data (headers included) into a text editor like Notepad. Save the resulting text file as "import.dat" (or whatever is in the
        /// IMPORT_FILENAME constant). Save the file in the application run directory.
        /// 
        /// The ImportFile class translates the import file into a list of Enrollee or Claim objects. Note that this process could be replaced by any means of
        /// producing the enrollee objects, so the text import process is purely for the sake of convenience and testing.
        /// 
        /// In the Main() method, have either ClaimFile(); or EnrollmentFile(); depending on what kind of file you are generating.
        /// 
        /// We use the EDI_Enrollment_File class to generate an EDI file from the Enrollee objects associated with that specific group. See the 
        /// <see cref="EDI_Enrollment_File.ToEDI(IEnumerable{Enrollee})"/> method for more details about the EDI conversion process.
        /// 
        /// The output files are saved to the application directory.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ClaimFile();
            //EnrollmentFile();
        }

        static void ClaimFile()
        {
            //process the import file
            List<Claim> claims = new ImportFile<Claim>().Import(IMPORT_FILENAME);

            //create submitter because this isn't in the import file (however, it'd be simple to use the import file
            //functionality to do so; however, my files were all from the same submitter)
            var submitter = new Submitter() {
                EIN = "SubmitterEIN",
                OrganizationName = "SubmitterORG",
                NPINumber = "NPI00000123",
                Type = EntityType.NonPerson,
                Address = new Address() {
                    State = "TX",
                    Street = "5250 N.Old Orchard Road",
                    City = "Skokie",
                    Zip = "60077-4462"
                },
                Phone = "123-456-4567",
                Email = "anemail@wchoiceinc.com"
            };

            //add submitter to each claim
            claims.ForEach(c => {
                c.Submitter = submitter;
            });

            //generate the file
            var claimFile = new EDI_Claim_File(new XMLControlNumbers(), DataType.Test);
            File.WriteAllText(claimFile.ExportFilename, claimFile.ToEDI(claims, submitter));
        }

        static void EnrollmentFile()
        {
            //process the import file
            List<Enrollee> enrollees = new ImportFile<Enrollee>().Import(IMPORT_FILENAME);

            //add effective date to enrollees without an effective date. Details like this are results
            //of my specific business needs and not something that would need to be done generally
            enrollees.ForEach(e => {
                if (e.EffectiveDate == default(DateTime))
                    e.EffectiveDate = DefaultEffectiveDate;
            });

            //break them into separate groups because a given file can only contain one group
            foreach (var enrolleeGroup in enrollees.GroupBy(e => e.EmploymentInfo.GroupID))
            {
                var ediFile = new EDI_Enrollment_File(new XMLControlNumbers(), DataType.Prod, enrolleeGroup.Key);

                //write the text to a file
                File.WriteAllText(ediFile.ExportFilename, ediFile.ToEDI(enrolleeGroup));
            }

            //generate the file with the groups combined
            var combinedEDIFile = new EDI_Enrollment_File(new XMLControlNumbers(), DataType.Test);
            File.WriteAllText(combinedEDIFile.ExportFilename, combinedEDIFile.ToEDI(enrollees));
        }
    }
}
