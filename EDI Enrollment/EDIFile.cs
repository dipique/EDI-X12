using System;
using System.Collections.Generic;
using System.Linq;

namespace EDI_Enrollment
{
    public abstract class EDIFile
    {
        public const string PARTNER_ID = "PARTNER_ID";
        public const string RECEIVER_ID = "RCVR_ID";
        public const string FLD_DELIM = "*";
        public const string END_OF_SEGMENT = "~";
        public const string REPETITION_DELIM = ">";
        public const string USAGE_DELIM = ":";
        public const string SENDER_TIN = "SENDER_TIN";      //in my implementation, all this was constant so it
        public const string RECEIVER_TIN = "RCVR_TIN";      //didn't need to be input on the front end
        public const string DT_FMT_D8 = "yyyyMMdd"; //export format is TEST/PROD D8
        public const string EXPORT_DT_FMT = "yyyyMMdd_hhmmss";

        public bool IncludeNewlineCharBetweenSegments { get; set; } = false;
        public string JoinString => IncludeNewlineCharBetweenSegments ? "\n" : string.Empty;

        internal abstract string VRI_ID_CODE { get; }
        internal abstract string TX_SET_ID { get; }

        public DataType DataType { get; set; }
        public ControlNumberProvider ControlNumbers { get; set; }

        internal string EDIVal<T>(T value) => EnumHelper<T>.GetDisplayValue(value);
        public abstract string ExportFilename { get; }

        //set up the variable to contain the file being built and the methods that add EDI lines. This is syntactical sugar
        //and allows the actual implementation to avoid the hassle of dealing with string joining, separators, and so on.
        internal List<string> EDILines = new List<string>();
        internal void AddEDILine(SegmentType segmentType, params string[] elements) => EDILines.Add($"{EDIVal(segmentType)}{FLD_DELIM}{string.Join(FLD_DELIM, elements)}{END_OF_SEGMENT}");
        internal void AddEDILine(params string[] elements) => EDILines.Add($"{string.Join(FLD_DELIM, elements)}{END_OF_SEGMENT}");

        
    
        /// <summary>
        /// Create the EDI object
        /// </summary>
        /// <param name="controlNumberProvider"></param>
        /// <param name="dataType"></param>
        public EDIFile(ControlNumberProvider controlNumberProvider, DataType dataType = DataType.Test)
        {
            DataType = dataType;
            ControlNumbers = controlNumberProvider;
        }
    }
}
