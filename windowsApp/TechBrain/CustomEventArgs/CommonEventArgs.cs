using System;

namespace TechBrain.CustomEventArgs
{
    /// <summary>
    /// Custom EventArgs with added property
    /// </summary>
    public class CommonEventArgs : EventArgs
    {
        public CommonEventArgs(object value)
        {
            Value = value;
        }

        public CommonEventArgs(object value, string message) : this(value)
        {
            Message = message;
        }

        public CommonEventArgs(object value, string message, bool? success) : this(value, message)
        {
            Success = success;
        }

        public object Value { get; private set; }
        public string Message { get; private set; }
        public bool? Success { get; private set; }
    }
}