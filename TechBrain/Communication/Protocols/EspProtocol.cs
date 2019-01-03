using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TechBrain.Communication.Drivers;

namespace TechBrain.Communication.Protocols
{
    public class EspProtocol : Protocol
    {
        public EspProtocol(IDriver driver) : base(driver)
        {
        }

        public override bool Ping()
        {
            try
            {
                using (var client = Driver.OpenClient())
                {                
                    client.Write("esp_ping()\n");
                    client.WaitResponse("OK: ping");
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }
    }
}
