using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var devServer = new DevServer(config, devices);
            devServer.Start();
            while (true)
            {

            }
        }
    }
}
