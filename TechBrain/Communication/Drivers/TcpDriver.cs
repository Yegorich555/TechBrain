using System.Net;
using System.Net.Sockets;

namespace TechBrain.Communication.Drivers
{
    public class TcpDriver : IDriver
    {
        private IPAddress ipAddress;
        private int? ipPort;

        public TcpDriver(IPAddress ipAddress, int? ipPort)
        {
            this.ipAddress = ipAddress;
            this.ipPort = ipPort;
        }

        public IDriverClient OpenClient()
        {
            var client = new TcpClient();
            client.SendTimeout = 500;
            client.ReceiveTimeout = 500;
            return new TcpDriverClient(client);
        }
    }
}
