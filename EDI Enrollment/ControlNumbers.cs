using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace EDI_Enrollment
{
    /// <summary>
    /// Control numbers identify EDI segments, and must increment across files. This class is intended
    /// to provide a way to assign incrementing control numbers in a test environment, while still
    /// implementing a base class that allows different, more scaleable methods to be employed without
    /// changing code that depends on it.
    /// 
    /// The major concession to this architecture is the <see cref="ControlNumberProvider.SaveControlNumbers"/>
    /// method. Many ways of generating control numbers would require a save. Oh well. :)
    /// </summary>
    public class XMLControlNumbers: ControlNumberProvider
    {
        private const string FILENAME = "controlnumbers.dat";
        private string XMLFilename = string.Empty;

        private Dictionary<string, int> controlNumbers;
        public Dictionary<string, int> ControlNumbers
        {
            get
            {
                if (controlNumbers == null)
                {
                    if (File.Exists(XMLFilename))
                    {
                        using (var sr = new StreamReader(XMLFilename))
                        {
                            var serializer = new XmlSerializer(typeof(item[]));
                            controlNumbers = ((item[])serializer.Deserialize(sr.BaseStream)).ToDictionary(i => i.id, i => i.value);
                        }
                    }
                    else
                    {
                        controlNumbers = new Dictionary<string, int>();
                    }
                }
                return controlNumbers;
            }
            set => controlNumbers = value;
        }

        public XMLControlNumbers(string filename = FILENAME) => XMLFilename = filename;

        public override int GetIncremented(string segment)
        {
            int retVal = 1;
            if (ControlNumbers.TryGetValue(segment, out int currentControlNumber))
                retVal = currentControlNumber + 1;                

            ControlNumbers[segment] = retVal;
            return retVal;
        }

        public override int GetCurrent(string segment) => GetCurrent(segment, false);
        public int GetCurrent(string segment, bool initializeIfNeeded) =>
            ControlNumbers.TryGetValue(segment, out int currentControlNumber) ? currentControlNumber
                                                                              : initializeIfNeeded ? GetIncremented(segment) 
                                                                                                   : -1;
        public override bool SaveControlNumbers()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(item[]));
                using (var sw = new StreamWriter(XMLFilename))
                {
                    serializer.Serialize(sw, ControlNumbers.Select(cn => new item() { id =  cn.Key, value = cn.Value }).ToArray());
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    public class item
    {
        public string id { get; set; }
        public int value { get; set; }
    }

    /// <summary>
    /// For performance, control numbers saves are done at the end, not for every single control number increment.
    /// </summary>
    public abstract class ControlNumberProvider
    {
        public abstract int GetIncremented(string segment);
        public abstract int GetCurrent(string segment);

        public string GetIncrementedStr(string segment, int padLength = -1, bool includePrefix = true) => GetString(segment, GetIncremented(segment), padLength, includePrefix);
        public string GetCurrentStr(string segment, int padLength = -1, bool includePrefix = true) => GetString(segment, GetCurrent(segment), padLength, includePrefix);

        private string GetString(string segment, int value, int padLength, bool includePrefix)
        {
            string retVal = value.ToString();
            string prefix = includePrefix ? segment : string.Empty;
            if (padLength > 1) retVal = retVal.PadLeft(padLength, '0');
            return retVal;
        }

        public abstract bool SaveControlNumbers();
    }
}
