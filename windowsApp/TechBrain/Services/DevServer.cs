using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TechBrain.Entities;
using TechBrain.Extensions;
using TechBrain.IO;

namespace TechBrain.Services
{
    public class DevServer : IDisposable
    {
        public event EventHandler<string> ErrorLog;
        public ConcurrentBag<Device> Devices;

        Config _config;
        TcpServer _tcpServer;        

        public DevServer(Config config, IList<Device> devices): this(config)
        {            
            Devices = new ConcurrentBag<Device>(devices);
        }

        public DevServer(Config config)
        {
            _config = config;
            if (Devices != null && FileSystem.ExistPath(config.PathDevices))
            {
                var text = File.ReadAllText(config.PathDevices);
                var devices = JsonConvert.DeserializeObject<List<Device>>(text);
                Devices = new ConcurrentBag<Device>(devices);
            }
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
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
            var json = JsonConvert.SerializeObject(Devices, settings);
            File.WriteAllText(_config.PathDevices, json);
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
                Devices = null;
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
