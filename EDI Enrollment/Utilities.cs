using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDI_Enrollment
{
    public static class Extensions
    {
        public static string ToNumeric(this String val) => val == null ? string.Empty : new string(val.Where(c => char.IsDigit(c)).ToArray());

        public static string PadToLength(this string value, int length, char rept = ' ')
        {
            if (length <= value.Length) return value;
            return $"{new string(rept, length - value.Length)}{value}";
        }
    }
}
