using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TechBrain;
using TechBrain.CustomEventArgs;
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
            devices.Add(new Device()
            {
                SerialNumber = 1,
                HasSleep = true,
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
                }
            });

            var sim = new Simulator(config, devices);
            sim.Start();

            var devServer = new DevServer(config, devices);
            devServer.ErrorLog += DevServer_ErrorLog;
            devServer.Start();

            sim.EspSend();
            while (true)
            {

            }
        }

        private static void DevServer_ErrorLog(object sender, CommonEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
