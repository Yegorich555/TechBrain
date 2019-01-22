using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain
{
    public class Config
    {
        public int TcpPort { get; private set; } = 1234;
        public int TcpResponseTimeout { get; private set; } = 5000;
        public int TcpEspPort { get; private set; } = 1999;//80;

        public string ComPort { get; private set; } = "COM3";
        public string PathDevices { get; private set; } = "devices.json";
    }
}
