using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using TechBrain;
using TechBrain.Entities;
using TechBrain.Extensions;
using TechBrain.Services;

namespace ConsoleLauncher
{
    class Simulator
    {
        private Config config;
        private List<Device> devices;
        TcpServer _tcpServer;
        public Simulator(Config config, List<Device> devices)
        {
            this.config = config;
            this.devices = devices;
        }

        public void Start()
        {
            _tcpServer = new TcpServer()
            {
                Port = 1999,
                ReceiveTimeout = 200,
                SendTimeout = 200,
                ThreadName = "Simulator_TcpListener",
            };
            _tcpServer.GotNewClient += _tcpServer_GotNewClient;
            _tcpServer.Start();

        }

        private void _tcpServer_GotNewClient(object sender, TcpClient client)
        {
            try
            {
                Debug.WriteLine("Simulator new client");
                var str = client.ReadLine();
                var result = (str.Contains("esp_") ? "OK: " : "Error: ") + str.Replace("esp_", "") + '\n';
                client.Client.Send(Encoding.ASCII.GetBytes(result));
                Debug.WriteLine($"Simulator get: '{str}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Simulator: " + ex);
            }
            finally
            {
                client?.Close();
                client?.Dispose();
            }
        }

        internal void EspSend()
        {
            try
            {
                var dev = devices.First(a => a.Type == DeviceTypes.ESP);
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = config.TcpReceiveTimeout;
                    client.Connect("localhost", config.TcpPort);
                    client.Write($"I am ({dev.SerialNumber})\n");
                    var str = client.ReadLine();
                    Debug.WriteLine($"Simulator. Esp get: '{str}'");
                }
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
