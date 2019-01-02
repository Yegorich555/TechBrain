using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TechBrain.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Return true if object type is string and empty
        /// </summary>
        /// <returns>Return true if object.IsString() and string.IsNullOrEmpty((string)object)</returns>
        public static bool IsNull(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }


        /// <summary>
        /// Substring between two chars
        /// </summary>
        public static string Extract(this string text, char startVal, char endVal, int startIndex = 0)
        {
            if (text == null)
                return null;
            var indStart = text.IndexOf(startVal, startIndex);
            if (indStart < 0)
                return null;
            var indEnd = text.IndexOf(endVal, startIndex);

            ++indStart;

            if (indEnd < 0)
                return text.Substring(indStart);
            else
            {
                var len = indEnd - indStart;
                return text.Substring(indStart, len);
            }
        }

        /// <summary>
        /// Return contains text result with case ignoring
        /// </summary>
        public static bool ContainsText(this string text, string value)
        {
            if (text == null)
                return false;
            var reg = new Regex(value, RegexOptions.IgnoreCase);
            return reg.IsMatch(text);
        }

        /// <summary>
        /// Compare two strings with case ignoring
        /// </summary>
        public static bool IsEqual(this string text, string value)
        {
            return string.Compare(text, value, true) == 0;
        }
    }
}
