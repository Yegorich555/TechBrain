using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TechBrain.Entities;
using TechBrain.Extensions;

namespace TechBrain.Services
{
    public class DevServer : IDisposable
    {
        public event EventHandler<string> ErrorLog;
        public DeviceRepository DeviceRepository { get; set; }

        Config _config;
        TcpServer _tcpServer;

        public DevServer(Config config, IEnumerable<Device> devices) : this(config)
        {
            DeviceRepository = new DeviceRepository(config.PathDevices, devices);
            DeviceRepository.Commit();
        }

        public DevServer(Config config)
        {
            _config = config;
            if (DeviceRepository == null)
                DeviceRepository = new DeviceRepository(config.PathDevices);
            
        }

        public void Start()
        {
            _tcpServer = new TcpServer
            {
                Port = _config.TcpPort,
                ReceiveTimeout = _config.TcpResponseTimeout / 2,
                SendTimeout = _config.TcpResponseTimeout / 2,
                ThreadName = "DevServer_ESP_TCP",
            };
            _tcpServer.GotNewClient += GotNewClient;
            _tcpServer.Start();
        }

        public void Stop()
        {
            if (_tcpServer != null)
            {
                _tcpServer.Stop();
                _tcpServer.GotNewClient -= GotNewClient;
            }
        }

        void GotNewClient(object sender, TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    Debug.WriteLine($"DevServer.ESP. New client: {client.Client.RemoteEndPoint}");
                    var str = client.WaitResponse("I am");
                    Debug.WriteLine($"DevServer.ESP. Parcel from {client.Client.RemoteEndPoint}: '{str}'");

                    var num = int.Parse(str.Extract('(', ')'));
                    var IpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    AddOrUpdate(IpAddress, num);

                    byte[] back = Encoding.ASCII.GetBytes("OK\n");
                    stream.Write(back, 0, back.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DevServer.ESP. Exception: " + ex);
                ErrorLog?.Invoke(this, "Tcp client exception:" + ex);
            }
            finally
            {
                client?.Close();
                client?.Dispose();
            }
        }

        void AddOrUpdate(IPAddress IpAddress, int SerialNumber)
        {
            var item = DeviceRepository.Get(v => v.Id == SerialNumber);
            if (item != null)
            {
                item.IpAddress = IpAddress;
                item.IsOnline = true;
            }
            else
            {
                DeviceRepository.Add(new Device()
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
                DeviceRepository = null;
                Stop();
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
