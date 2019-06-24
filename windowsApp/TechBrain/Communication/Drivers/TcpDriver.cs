using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TechBrain.Extensions;

namespace TechBrain.Communication.Drivers
{
    public class TcpDriver : IDriver
    {
        const int CloseDelay = 300;//ms

        internal static ConcurrentDictionary<string, Client> openedClients = new ConcurrentDictionary<string, Client>();

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
            var key = ipAddress + ":" + ipPort;
            if (openedClients.TryGetValue(key, out var result) && result.Connected) //todo Connected is not always the truth
            {
                result.responseTimeout = ResponseTimeout;
                result.BreakDispose();
                return result;
            }
            var client = new TcpClient();
            var sw = new Stopwatch();
            sw.Start();
            if (!client.ConnectAsync(ipAddress, ipPort).Wait(ResponseTimeout))
                throw new TimeoutException($"Tcp open timeout: {ResponseTimeout}ms to {ipAddress.ToStringNull()}:{ipPort}");
            sw.Stop();

            var remainedTimeout = ResponseTimeout - Convert.ToInt32(sw.ElapsedMilliseconds);
            result = new Client(client, remainedTimeout, key);
            openedClients.TryAdd(key, result);
            return result;
        }

        public class Client : IDriverClient
        {
            internal int responseTimeout;
            private int writeTime;
            private TcpClient client;
            private string key;

            public Client(TcpClient client, int responseTimeout, string key)
            {
                this.client = client;
                this.responseTimeout = responseTimeout;
                this.key = key;
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

            CancellationTokenSource tokenSource;// = new CancellationTokenSource();

            public bool Connected { get => client.Connected; }

            //dispose with delay
            public void Dispose()
            {
                tokenSource = new CancellationTokenSource();
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(CloseDelay), tokenSource.Token);
                        if (!tokenSource.Token.IsCancellationRequested)
                        {
                            openedClients.TryRemove(key, out var value);
                            client.Close();
                            client.Dispose();
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex);//todo logger
                    }
                }, tokenSource.Token);
            }

            internal void BreakDispose()
            {
                tokenSource.Cancel();
            }
        }
    }
}
