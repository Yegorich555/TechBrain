using System;
using System.Diagnostics;
using TechBrain;
using TechBrain.CustomEventArgs;
using TechBrain.Drivers.Uart;
using TechBrain.Extensions;
using TechBrain.Services;

namespace ConsoleLauncher
{
    class Program
    {
        DevServer devServer;
        static Config config = new Config();
        static void Main(string[] args)
        {
            var devServer = new DevServer(config);
            devServer.Start();
            while (true)
            {

            }
        }
    }
}
