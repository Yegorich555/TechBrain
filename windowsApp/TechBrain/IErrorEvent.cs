using System;
using TechBrain.CustomEventArgs;
using TechBrain.Extensions;

namespace TechBrain
{
    public class IError : IErrorEvent
    {
        public event EventHandler<CommonEventArgs> ErrorAppeared;

        protected virtual void OnErrorAppeared(Exception ex)
        {
            OnErrorAppeared(ex.ToString());
        }
        protected virtual void OnErrorAppeared(params object[] arr)
        {
            OnErrorAppeared(Extender.BuildString(arr));
        }
        protected virtual void OnErrorAppeared(string str)
        {
            OnErrorAppeared(new CommonEventArgs(null, str));
        }
        protected virtual void OnErrorAppeared(CommonEventArgs args)
        {
            ErrorAppeared?.Invoke(this, args);
        }

    }

    public interface IErrorEvent
    {
        event EventHandler<CommonEventArgs> ErrorAppeared;
    }

}
