using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public ConcurrentBag<Device> Devices;

        Thread _thread;
        Config _config;
        public DevServer(Config config, IList<Device> devices)
        {
            _config = config;
            Devices = new ConcurrentBag<Device>(devices);
        }

        public volatile static bool LoopEnd;

        public void Start()
        {
            TcpListen(_config.TcpPort, _config.TcpReceiveTimeout);
        }


        void TcpListen(int port, int receiveTimeout)
        {
            LoopEnd = true;

            _thread = new Thread(() =>
            {
                LoopEnd = false;
                var server = new TcpListener(IPAddress.Any, port); //todo port+1, port+2 if this is busy
                server.Start();
                Loop(server, receiveTimeout);
                server.Stop();
            });

            _thread.Name = "DevServerTCP";
            _thread.Start();

        }

        void Loop(TcpListener server, int receiveTimeout)
        {
            while (true && !LoopEnd)
            {
                if (!server.Pending())
                    continue;

                Task.Run(() =>
                {
                    try
                    {
                        using (var client = server.AcceptTcpClient())
                        {
                            client.ReceiveTimeout = receiveTimeout;
                            using (var stream = client.GetStream())
                            {
                                var str = client.ReadLine();
                                var i = str.IndexOf("I am");
                                if (i == -1)
                                {
                                    ErrorLog?.Invoke(this, new CommonEventArgs(null, "Wrong parcel: " + str));
                                    return;
                                }
                                var IpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                                Debug.WriteLine($"DevServer. Parcel from TCP ({IpAddress}): '{str}'");

                                var num = int.Parse(str.Extract('(', ')', i));
                                AddOrUpdate(IpAddress, num);

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
        }

        void AddOrUpdate(IPAddress IpAddress, int SerialNumber)
        {
            var item = Devices.FirstOrDefault(v => v.SerialNumber == SerialNumber);
            if (item != null)
            {
                item.IpAddress = IpAddress;
                item.IsOnline = true;
            }
            else
            {
                Devices.Add(new Device()
                {
                    Type = DeviceTypes.ESP,
                    HasResponse = true,
                    HasSleep = true,
                    IpAddress = IpAddress,
                    SerialNumber = SerialNumber,
                    IsOnline = true,
                });
            }
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
                LoopEnd = true;
                Devices = null;
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
