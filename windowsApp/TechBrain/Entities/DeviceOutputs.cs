using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Entities
{
    public enum OutputTypes
    {
        None,
        Digital,
        Pwm,
    }

    public class DeviceOutput: IEntity
    {
        public int SerialNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public OutputTypes Type { get; set; }
        public int? Value { get; private set; }

        public override string ToString()
        {
            return SerialNumber + "-" + Name + ": " + Value?.ToString();
        }
    }
}
