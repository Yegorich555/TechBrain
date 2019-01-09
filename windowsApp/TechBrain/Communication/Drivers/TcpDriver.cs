using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public int ResponseTimeout { get; set; } = 1000;

        public IDriverClient OpenClient()
        {
            var client = new TcpClient
            {
                SendTimeout = ResponseTimeout / 2,
                ReceiveTimeout = ResponseTimeout / 2
            };
            client.Connect(ipAddress, ipPort);
            return new Client(client, ResponseTimeout);
        }

        public class Client : IDriverClient
        {
            private readonly int responseTimeout;
            private long writeTime;
            private TcpClient client;

            public Client(TcpClient client, int responseTimeout)
            {
                this.client = client;
                this.responseTimeout = responseTimeout;
            }

            T WrapRead<T>(Func<T> func)
            {
                var sw = new Stopwatch();
                sw.Start();
                var v = func();
                sw.Stop();
                var responseTime = sw.ElapsedMilliseconds + writeTime;
                writeTime = 0;
                if (responseTime >= responseTimeout)
                    throw new TimeoutException($"Response timeout: {responseTimeout} ms");
                return v;
            }

            void WrapWrite(Action func)
            {
                var sw = new Stopwatch();
                sw.Start();
                func();
                sw.Stop();
                writeTime = sw.ElapsedMilliseconds;
            }

            public IList<byte> Read(byte? startByte, byte? endByte, int maxParcelSize = 255) => WrapRead(() => client.Read(startByte, endByte, maxParcelSize));
            public string WaitResponse(string v) => WrapRead(() => client.WaitResponse(v));

            public void Write(string v) => WrapWrite(() => client.Write(v));
            public void Write(IEnumerable<byte> bt) => WrapWrite(() => client.Write(bt));

            public void Dispose()
            {
                client.Close();
                client.Dispose();
            }
        }
    }
}
