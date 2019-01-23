using System;
using System.Threading;
using TechBrain.CustomEventArgs;

namespace TechBrain
{
    public class AsyncTimer : IDisposable
    {
        Timer threadTimer;
        /// <summary>
        /// Event for end cycle work of timer
        /// </summary>
        public event EventHandler<CommonEventArgs> CallBack;

        /// <summary>
        /// Init System.Threading.Timer
        /// </summary>
        public AsyncTimer() { }

        /// <summary>
        /// Init System.Threading.Timer with timeInterval
        /// </summary>
        /// <param name="timeInterval"></param>
        public AsyncTimer(int timeInterval)
        {
            Interval = timeInterval;
        }

        /// <summary>
        /// Init System.Threading.Timer with timeInvterval
        /// </summary>
        /// <param name="timeInterval"></param>
        /// <param name="arguments"></param>
        public AsyncTimer(int timeInterval, object arguments) : this(timeInterval)
        {
            Arguments = arguments;
        }

        #region Methods

        /// <summary>
        /// Start async timer
        /// </summary>
        public void Start()
        {
            StartStop(true, false);
        }
        /// <summary>
        /// Start async timer and Imediately call CallBack event
        /// </summary>
        public void StartImmediately()
        {
            StartStop(true);
        }
        /// <summary>
        /// Stop async timer
        /// </summary>
        public void Stop()
        {
            StartStop(false);
        }

        void StartStop(bool enable, bool immediately = true)
        {
            enabled = enable;
            if (enable)
            {
                int dueTime = 0;
                if (!immediately)
                    dueTime = Interval;
                if (threadTimer == null)
                    threadTimer = new Timer(timerCallBack, null, dueTime, Interval);
                else
                    threadTimer.Change(dueTime, Interval);
            }
            else if (threadTimer != null)
                threadTimer.Change(-1, -1);
        }

        private void timerCallBack(object state)
        {
            CallBack?.Invoke(this, new CommonEventArgs(Arguments));
        }

        /// <summary>
        /// Stop and dispose timer
        /// </summary>
        public void Dispose()
        {
            if (threadTimer != null)
            {
                Stop();
                threadTimer.Dispose();
                threadTimer = null;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Timer between events CallBack
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Arguments as saved element
        /// </summary>
        public object Arguments { get; set; }

        bool enabled;
        /// <summary>
        /// Enable timer
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if (enabled == value)
                    return;

                enabled = value;
                StartStop(enabled);
            }
        }

        #endregion

    }
}