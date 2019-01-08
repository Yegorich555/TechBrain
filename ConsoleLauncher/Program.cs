using System;
using System.Collections.Generic;
using System.Threading;
using TechBrain;
using TechBrain.Entities;
using TechBrain.Services;

namespace ConsoleLauncher
{
    class Program
    {
        static Config config = new Config();
        static List<Device> devices = new List<Device>();
        static void Main(string[] args)
        {
            devices.Add(new Device()
            {
                SerialNumber = 1,
                HasSleep = true,
                HasResponse = true,
                HasTime = true,
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

            var sim = new Simulator(config, devices);
            sim.Start();

            var devServer = new DevServer(config, devices);
            devServer.ErrorLog += (object s, string e) => Console.WriteLine(e);
            devServer.Start();

            Thread.Sleep(20);
            sim.EspSend();
            while (true)
            {
                Console.WriteLine("ping: " + devices[0].Ping());
                Thread.Sleep(2000);
            }
        }
    }
}
