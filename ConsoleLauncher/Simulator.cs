using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TechBrain;
using TechBrain.Entities;
using TechBrain.Extensions;

namespace ConsoleLauncher
{
    class Simulator
    {
        private Config config;
        private List<Device> devices;

        public Simulator(Config config, List<Device> devices)
        {
            this.config = config;
            this.devices = devices;
        }

        public void Start()
        {
            Task.Run(() =>
            {
                var server = new TcpListener(IPAddress.Any, 1999);
                server.Start();
                while (true)
                {
                    if (!server.Pending())
                        continue;

                    Task.Run(() =>
                    {
                        try
                        {
                            using (var client = server.AcceptTcpClient())
                            {
                                client.ReceiveTimeout = 200;
                                var str = client.ReadLine();
                                Console.WriteLine($"Simulator get: '${str}'");
                                var result = str.Contains("esp_") ? "OK: " : "Error: " + str.Replace("esp_", "");
                                client.Client.Send(Encoding.ASCII.GetBytes(result));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Simulator: " + ex);
                        }
                    });
                }
            });
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
                    Console.WriteLine($"Simulator. Esp get: '{str}'");
                }
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
