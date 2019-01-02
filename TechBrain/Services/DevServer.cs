using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TechBrain.CustomEventArgs;
using TechBrain.Entities;
using TechBrain.Extensions;

namespace TechBrain.Services
{
    public class DevServer : IDisposable
    {
        public event EventHandler<CommonEventArgs> ErrorLog;
        public ConcurrentDictionary<int, ESP8266_Device> ESP8266_Devices = new ConcurrentDictionary<int, ESP8266_Device>();

        Thread _thread;
        Config _config;
        public DevServer(Config config)
        {
            _config = config;
        }

        public void Start()
        {
            TcpListen(_config.TcpPort, _config.TcpReceiveTimeout);
        }

        void TcpListen(int port, int receiveTimeout)
        {
            var server = new TcpListener(IPAddress.Any, port);
            server.Start();

            _thread = new Thread(() =>
            {
                while (true)
                {
                    if (!server.Pending())
                        break;

                    Task.Run(() =>
                    {
                        try
                        {
                            using (var client = server.AcceptTcpClient())
                            {
                                client.ReceiveTimeout = receiveTimeout;
                                using (var stream = client.GetStream())
                                {
                                    using (var reader = new StreamReader(stream, Encoding.ASCII))
                                    {
                                        var str = reader.ReadLine();
                                        var i = str.IndexOf("I am");
                                        if (i == -1)
                                        {
                                            ErrorLog?.Invoke(this, new CommonEventArgs(null, "Wrong parcel: " + str));
                                            return;
                                        }
                                        var IpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                                        Debug.WriteLine($"DevServer. Parcel from TCP ({IpAddress}): '${str}'; ");

                                        var num = int.Parse(str.Extract('(', ')', i));
                                        ESP8266_Devices.AddOrUpdate(num, new ESP8266_Device()
                                        {
                                            IpAddress = IpAddress,
                                            Number = num
                                        },
                                        (n, val) =>
                                        {
                                            val.IpAddress = IpAddress;
                                            return val;
                                        });

                                    }
                                    byte[] back = Encoding.ASCII.GetBytes("OK\n");
                                    stream.Write(back, 0, back.Length);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorLog?.Invoke(this, new CommonEventArgs(null, "Tcp client exception:" + ex));
                        }
                    });
                }
            });

            _thread.Name = "DevServerTCP";
            _thread.Start();

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                ESP8266_Devices = null;
                _thread = null;
                disposedValue = true;
                _config = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
