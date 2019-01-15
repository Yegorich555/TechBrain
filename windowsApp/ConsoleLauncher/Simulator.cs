using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TechBrain;
using TechBrain.Communication.Protocols;
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

                using (var stream = client.GetStream())
                {
                    Thread.Sleep(20);
                    var buf = new byte[255];
                    var count = stream.Read(buf, 0, buf.Length);
                    var bytes = buf.Take(count).ToArray();

                    var str = Encoding.ASCII.GetString(bytes);
                    if (str.Contains("esp_"))
                    {
                        var result = (str.Contains("esp_") ? "OK: " : "Error: ") + str.Replace("esp_", "") + '\n';
                        if (!str.Contains("sleep"))
                            stream.Write(Encoding.ASCII.GetBytes(result));
                        Debug.WriteLine($"Simulator get: '{str.Replace("\n", "/n")}'");
                    }
                    else
                    {
                        var address = TbProtocol.ExtractAddressFrom(bytes);
                        Debug.WriteLine($"Simulator get from '{address}'");
                        var bt = TbProtocol.GetResponse(bytes.ToList());
                        stream.Write(bt.ToArray());
                    }
                }
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

        internal void EspSend(int number)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect("localhost", config.TcpPort);
                    client.Write($"I am ({number})\n");

                    client.ReceiveTimeout = config.TcpResponseTimeout;
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
