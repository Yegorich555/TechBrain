using System;

namespace TechBrain.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Convert time-span to user-friendly dynamic string format like '321h:12m' or '12m:11s' or '12s'
        /// </summary>
        public static string ToStringFriendly(this TimeSpan span, string nameHour = "h", string nameMinute = "m", string nameSecond = "s")
        {
            var totalHours = span.TotalHours;
            if (totalHours > 0)
            {
                if (totalHours > short.MaxValue)
                    return Extender.BuildString("> " + short.MaxValue, nameHour);
                return Extender.BuildString(Convert.ToInt16(totalHours), nameHour,':', span.Minutes, nameMinute);
            }
            else if (span.Minutes > 0)
            {
                return Extender.BuildString(span.Minutes, nameMinute, ':', span.Seconds, nameSecond);
            }
            else
            {
                return Extender.BuildString(span.Seconds, nameSecond);
            }
        }

        public static Type GetTypeNull(this object obj)
        {
            if (obj == null)
                return typeof(object);
            else return obj.GetType();
        }

        public static string ToStringNull(this object obj, string nullText = "")
        {
            if (obj == null)
                return nullText;
            else return obj.ToString();
        }

    }
}
