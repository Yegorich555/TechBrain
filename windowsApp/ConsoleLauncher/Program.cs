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
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "TechBrain";
                Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

                var config = Config.ReadFromFile();
                var devServer = new DevServer(config);
                devServer.Start();

                var devices = devServer.DeviceRepository.GetAll();
                Console.Title = "TechBrain - Started";
                Console.ReadLine();
                //while (true)
                //{

                //}

                config.SaveToFile();
                devServer.DeviceRepository.Commit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
