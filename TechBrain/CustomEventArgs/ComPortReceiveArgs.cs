using System;
using System.Collections.Generic;

namespace TechBrain.CustomEventArgs
{
    public class ComPortReceiveArgs : EventArgs
    {
        public ComPortReceiveArgs(IList<byte> value)
        {
            Bytes = value;
        }
        public IList<byte> Bytes { get; private set; }
    }
}