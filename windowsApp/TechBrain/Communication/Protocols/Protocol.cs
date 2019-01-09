using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechBrain.Communication.Drivers;
using TechBrain.Entities;

namespace TechBrain.Communication.Protocols
{
    public abstract class Protocol
    {
        public IDriver Driver { get; private set; }
        public Protocol(IDriver driver)
        {
            Driver = driver;
        }
        //bool UpdateSensors(IDriver driver, IList<Sensor> sensors);
        public abstract bool SetTime(DateTime dt);
        public abstract bool Ping();        
    }
}
