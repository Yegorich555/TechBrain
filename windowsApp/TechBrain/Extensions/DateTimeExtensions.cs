using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime TruncateToSeconds(this DateTime dateTime)
        {
            return Truncate(dateTime, TimeSpan.FromSeconds(1));
        }

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            //if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            //if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // do not modify "guard" values
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
    }
}
