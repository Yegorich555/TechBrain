﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TechBrain;
using TechBrain.Communication.Protocols;
using TechBrain.Entities;
using TechBrain.Extensions;
using TechBrain.Services;

namespace ConsoleTest
{
    class Simulator
    {
        private DevServerConfig config;
        private readonly List<Device> devices;
        TcpServer _tcpServer;
        public Simulator(DevServerConfig config, List<Device> devices)
        {
            this.config = config;
            this.devices = devices;
        }

        public void Start()
        {
            _tcpServer = new TcpServer()
            {
                Port = config.Esp_TcpPort,
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
                Trace.WriteLine("Simulator new client");

                using (var stream = client.GetStream())
                {
                    while (client.Connected)
                    {
                        if (client.Available < 1)
                            continue;
                        Thread.Sleep(100);
                        var buf = new byte[255];
                        var count = stream.Read(buf, 0, buf.Length);
                        var bytes = buf.Take(count).ToArray();

                        var str = Encoding.ASCII.GetString(bytes);
                        if (str.Contains("esp_"))
                        {
                            Task task = null;
                            var result = (str.Contains("esp_") ? "OK: " : "Error: ") + str.Replace("esp_", "") + '\n';
                            if (!str.Contains("sleep"))
                                stream.Write(Encoding.ASCII.GetBytes(result));
                            else
                                task = Task.Run(async () =>
                                  {
                                      var sec = int.Parse(str.Extract('(', ')'));
                                      Trace.WriteLine($"Simulator is sleeping for {sec}sec");
                                      await Task.Delay(TimeSpan.FromSeconds(sec));
                                      Trace.WriteLine($"Simulator is woken up:");
                                      EspSend(devices[0].SerialNumber);
                                      EspSend(devices[1].SerialNumber);

                                  });
                            Trace.WriteLine($"Simulator get: '{str.Replace("\n", "/n")}'");
                            if (task != null)
                                task.Wait();
                        }
                        else
                        {
                            var address = TbProtocol.ExtractAddressFrom(bytes);
                            Trace.WriteLine($"Simulator get from '{address}'");
                            var bt = TbProtocol.GetResponse(bytes.ToList());
                            stream.Write(bt.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Simulator: " + ex);
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
                    client.Connect("localhost", config.DevServer_TcpPort);
                    client.Write($"I am ({number})\n");

                    client.ReceiveTimeout = config.TcpResponseTimeout;
                    var str = client.ReadLine();
                    Trace.WriteLine($"Simulator. Esp get: '{str}'");
                }
            }
            catch (TimeoutException ex)
            {
                Trace.WriteLine(ex);
            }
        }

        public static List<Device> GenerateNewDevices(DevServerConfig config)
        {
            var lst = new List<Device>();
            lst.Add(new Device(config.DeviceCacheTime)
            {
                Id = 1,
                SerialNumber = 1,
                SleepTime = TimeSpan.FromMinutes(1),
                HasResponse = true,
                HasTime = false,
                Name = "FirstESP",
                Type = DeviceTypes.ESP,
                Outputs = new List<Output>()
                {
                   new Output()
                   {
                       Id = 1,
                       Name = "TestOut1",
                       Type = OutputTypes.Pwm,
                   }
                },
                ResponseTimeout = 2000,
                IpPort = config.Esp_TcpPort,
            });
            lst.Add(new Device(config.DeviceCacheTime)
            {
                Id = 2,
                SerialNumber = 2,
                SleepTime = TimeSpan.FromMinutes(1),
                HasResponse = true,
                HasTime = true,
                Name = "FirstESP_AVR",
                Type = DeviceTypes.ESP_AVR,
                Outputs = new List<Output>()
                {
                    new Output()
                   {
                       Id = 1,
                       Name = "TestOut1",
                       Type = OutputTypes.Digital,
                   }
                },
                Sensors = new List<Sensor>(),
                ResponseTimeout = 5000,
                IpPort = config.Esp_TcpPort,
            });

            for (int i = 0; i < 4; ++i)
            {
                var sensor = new Sensor()
                {
                    Id = i + 1,
                    Name = "Sensor " + i + 1,
                };
                lst[1].Sensors.Add(sensor);
            }

            return lst;
        }
    }
}
