using System;
using System.Diagnostics;
using TechBrain.CustomEventArgs;

namespace TechBrain.Services
{
    public class DateTimeService
    {
        static Stopwatch sw = new Stopwatch();

        public event EventHandler<CommonEventArgs> YearChanged;
        public event EventHandler<CommonEventArgs> MonthChanged;
        public event EventHandler<CommonEventArgs> DayChanged;
        public event EventHandler<CommonEventArgs> HourChanged;
        public event EventHandler<CommonEventArgs> MinuteChanged;

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

                    var args = new CommonEventArgs(now);
                    MinuteChanged?.Invoke(this, args);

                    if (now.Hour != lastNow.Hour)
                        HourChanged?.Invoke(this, args);
                    if (now.Day != lastNow.Day)
                        DayChanged?.Invoke(this, args);
                    if (now.Month != lastNow.Month)
                        MonthChanged?.Invoke(this, args);
                    if (now.Year != lastNow.Year)
                        YearChanged?.Invoke(this, args);
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
