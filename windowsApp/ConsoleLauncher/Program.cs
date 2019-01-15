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

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            var devServer = new DevServer(config);
            if (devServer.Devices == null)
                devServer = new DevServer(config, Simulator.GenerateNewDevices(config));
            devServer.Start();

            var sim = new Simulator(config, devServer.Devices.ToList());
            sim.Start();

            devServer.Stop();
            devServer.Start();

            Thread.Sleep(100);

            var devices = devServer.Devices.ToList();
            while (true)
            {
                sim.EspSend(devices[0].SerialNumber);
                sim.EspSend(devices[1].SerialNumber);
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
