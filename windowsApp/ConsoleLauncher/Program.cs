using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using TechBrain;
using TechBrain.Entities;
using TechBrain.Extensions;
using TechBrain.Services;

namespace ConsoleLauncher
{
    class Program
    {
        static Config config = new Config();
        static List<Device> devices = new List<Device>();

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            devices.Add(new Device()
            {
                SerialNumber = 1,
                HasSleep = true,
                HasResponse = true,
                HasTime = false,
                Name = "FirstESP",
                Type = DeviceTypes.ESP,
                Outputs = new List<DeviceOutput>()
                {
                   new DeviceOutput()
                   {
                       SerialNumber = 1,
                       Name = "TestOut1",
                       Type = OutputTypes.Pwm,
                   }
                },
                ResponseTimeout = 200,
                IpPort = config.TcpEspPort,
            });
            devices.Add(new Device()
            {
                SerialNumber = 2,
                HasSleep = true,
                HasResponse = true,
                HasTime = true,
                Name = "FirstESP_AVR",
                Type = DeviceTypes.ESP_AVR,
                Outputs = new List<DeviceOutput>()
                {
                    new DeviceOutput()
                   {
                       SerialNumber = 1,
                       Name = "TestOut1",
                       Type = OutputTypes.Digital,
                   }
                },
                Sensors = new List<Sensor>(),
                ResponseTimeout = 500,
                IpPort = config.TcpEspPort,
            });

            for (int i = 0; i < 4; ++i)
            {
                var sensor = new Sensor()
                {
                    SerialNumber = i + 1,
                    Name = "Sensor " + i + 1,
                };
                devices[1].Sensors.Add(sensor);
            }

            var sim = new Simulator(config, devices);
            sim.Start();

            var devServer = new DevServer(config, devices);
            devServer.ErrorLog += (object s, string e) => Console.WriteLine(e);
            devServer.Start();

            Thread.Sleep(20);
            sim.EspSend(devices[0].SerialNumber);
            sim.EspSend(devices[1].SerialNumber);
            while (true)
            {
                Console.WriteLine("ping Esp: " + devices[0].Ping());
                Console.WriteLine("ping Esp_Avr: " + devices[1].Ping());
                Console.WriteLine("time Esp_Avr: " + devices[1].SetTime(DateTime.Now));
                Console.WriteLine("sensors Esp_Avr: " + devices[1].UpdateSensors() + "=>" + Extender.JoinToString("; ", devices[1].Sensors.Select(v => v.Value)));
                Console.WriteLine("outputs Esp: " + devices[0].SetOut(1, 23));
                Console.WriteLine("outputs Esp_Avr: " + devices[1].SetOut(1, 100));
                Console.WriteLine("sleep Esp: " + devices[0].Sleep(TimeSpan.FromSeconds(10)));
                Console.WriteLine("sleep Esp_Avr: " + devices[1].Sleep(TimeSpan.FromSeconds(40)));
                Thread.Sleep(2000);
            }
        }
    }
}
