using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TechBrain.Extensions;

namespace TechBrain.Communication.Drivers
{
    public class TcpDriver : IDriver
    {
        private readonly IPAddress ipAddress;
        private int ipPort;

        public TcpDriver(IPAddress ipAddress, int ipPort)
        {
            this.ipAddress = ipAddress;
            this.ipPort = ipPort;
        }

        public int WriteTimeout { get; set; } = 1000;
        public int ReadTimeout { get; set; } = 1000;

        public IDriverClient OpenClient()
        {
            var client = new TcpClient();
            client.SendTimeout = WriteTimeout;
            client.ReceiveTimeout = ReadTimeout;
            client.Connect(ipAddress, ipPort);
            return new Client(client);
        }

        public class Client : IDriverClient
        {
            private TcpClient client;

            public Client(TcpClient client)
            {
                this.client = client;
            }

            public IList<byte> Read(byte? startByte, byte? endByte, int maxParcelSize = 255) => client.Read(startByte, endByte, maxParcelSize);
            public void WaitResponse(string v) => client.WaitResponse(v);
            public void Write(string v) => client.Write(v);
            public void Write(IEnumerable<byte> bt) => client.Write(bt);
            public void Dispose()
            {
                client.Close();
                client.Dispose();
            }
        }
    }
}
