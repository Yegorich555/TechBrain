using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechBrain.CustomEventArgs;

namespace TechBrain
{
    public class AsyncTimerAction
    {
        static AsyncTimer timer;

        public AsyncTimerAction(TimeSpan interval, Action action) : this(interval, false, action)
        {

        }

        public AsyncTimerAction(TimeSpan interval, bool skipEnable, Action action)
        {
            Action = action;
            SkipEnable = skipEnable;
            this.interval = interval;

            timer = new AsyncTimer(Convert.ToInt32(interval.TotalMilliseconds));
            timer.CallBack += Timer_CallBack;
        }

        public Action Action { get; set; }
        public bool SkipEnable { get; set; }

        TimeSpan interval;
        public TimeSpan Interval
        {
            get
            {
                return interval;
            }
            set
            {
                if (interval == value)
                    return;
                timer.Stop();
                timer.Interval = Convert.ToInt32(interval.TotalMilliseconds);
                timer.Start();
            }
        }


        volatile bool locked;
        private void Timer_CallBack(object sender, CommonEventArgs e)
        {
            if (locked)
                return;

            if (SkipEnable)
                locked = true;

            Action();

            locked = false;
        }

        public void StartImmidiately()
        {
            timer.StartImmediately();
        }

        public void Start()
        {
            timer.Start();
        }

        public void Dispose()
        {
            if (timer != null)
                timer.Dispose();
        }
    }
}
