using System;
using System.Diagnostics;
using TechBrain.CustomEventArgs;

namespace TechBrain.Services
{
    public class DateTimeService
    {
        static Stopwatch sw = new Stopwatch();

        public event EventHandler<DateTime> YearChanged;
        public event EventHandler<DateTime> MonthChanged;
        public event EventHandler<DateTime> DayChanged;
        public event EventHandler<DateTime> HourChanged;
        public event EventHandler<DateTime> MinuteChanged;

        private static DateTimeService instance;
        public static DateTimeService Instance
        {
            get
            {
                if (instance == null)
                    instance = new DateTimeService();
                return instance;
            }
        }

        AsyncTimerAction tPeriodical;
        DateTime lastNow;
        private DateTimeService()
        {
            lastNow = Now;
            tPeriodical = new AsyncTimerAction(TimeSpan.FromSeconds(1), () =>
            {
                now = Now;
                if (now.Minute != lastNow.Minute)
                {
                    tPeriodical.Interval = TimeSpan.FromMinutes(1);

                    MinuteChanged?.Invoke(this, now);

                    if (now.Hour != lastNow.Hour)
                        HourChanged?.Invoke(this, now);
                    if (now.Day != lastNow.Day)
                        DayChanged?.Invoke(this, now);
                    if (now.Month != lastNow.Month)
                        MonthChanged?.Invoke(this, now);
                    if (now.Year != lastNow.Year)
                        YearChanged?.Invoke(this, now);
                }
                
                lastNow = now;
            });
            tPeriodical.Start();
        }

        static DateTime now;
        /// <summary>
        /// Get DateTime.Now with cacheTime 1 sec
        /// </summary>
        public static DateTime Now
        {
            get
            {
                if (!sw.IsRunning || sw.Elapsed >= TimeSpan.FromSeconds(1))
                {
                    var now = NowCurrently;
                    sw.Restart();
                }
                return now;
            }
        }

        /// <summary>
        /// Get the currently now
        /// </summary>
        public static DateTime NowCurrently
        {
            get
            {
                now = DateTime.Now;
                return now;
            }
        }
    }
}
