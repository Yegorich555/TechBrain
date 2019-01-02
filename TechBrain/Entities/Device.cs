using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Entities
{
    public class Device
    {
        public int Number { get; set; }

        public virtual void ChangeOuts(int value)
        { }
        public virtual void ChangeOut(int num, int value)
        { }

        public virtual void GetSensors()
        { }

        public virtual void SyncTime()
        { }
        // public Device Destination { get; set; }
    }
}
