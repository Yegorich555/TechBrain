using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain
{
    public class Config
    {
        public int DevServer_TcpPort { get; private set; } = 1234;
        public int TcpResponseTimeout { get; private set; } = 1000;
        public int Esp_TcpPort { get; private set; } = 1999;//80;

        public string ComPort { get; private set; } = "COM3";
        public string PathDevices { get; private set; } = "devices.json";
    }
}
