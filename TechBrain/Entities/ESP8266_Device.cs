using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Entities
{
    public class ESP8266_Device : Device
    {
        public IPAddress IpAddress { get; set; }
        public override void ChangeOut(int num, int value)
        { }

    }
}
