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
            var client = new TcpClient();

            var sw = new Stopwatch();
            sw.Start();
            if (!client.ConnectAsync(ipAddress, ipPort).Wait(ResponseTimeout))
                throw new TimeoutException($"Tcp open timeout: {ResponseTimeout}ms to {ipAddress.ToStringNull()}:{ipPort}");
            sw.Stop();

            var remainedTimeout = ResponseTimeout - Convert.ToInt32(sw.ElapsedMilliseconds);
            return new Client(client, remainedTimeout);
        }

        public class Client : IDriverClient
        {
            private readonly int responseTimeout;
            private int writeTime;
            private TcpClient client;

            public Client(TcpClient client, int responseTimeout)
            {
                this.client = client;
                this.responseTimeout = responseTimeout;
            }

            T WrapRead<T>(Func<T> func)
            {
                client.ReceiveTimeout -= writeTime;
                var v = func();
                writeTime = 0;
                return v;
            }

            void WrapWrite(Action func)
            {
                client.SendTimeout = responseTimeout;
                var sw = new Stopwatch();
                sw.Start();
                func();
                sw.Stop();
                writeTime = sw.ElapsedMilliseconds <= int.MaxValue ? (int)sw.ElapsedMilliseconds : int.MaxValue;
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
