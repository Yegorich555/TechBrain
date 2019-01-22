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

        static void Wrap(string msg, Action action, Func<string> msgAction = null)
        {
            try
            {
                Console.WriteLine();
                Console.Write("-----");
                Console.Write(msg);
                Console.Write(": ");
                action();
                if (msgAction != null)
                    Console.Write(msgAction());
                Console.WriteLine();
            }
            catch (DeviceException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

                var devServer = new DevServer(config);
                if (devServer.DeviceRepository.Count == 0)
                    devServer = new DevServer(config, Simulator.GenerateNewDevices(config));
                devServer.Start();

                var devices = devServer.DeviceRepository.GetAll();
                var sim = new Simulator(config, devices);
                sim.Start();

                Thread.Sleep(100);

                while (true)
                {
                    sim.EspSend(devices[0].SerialNumber);
                    sim.EspSend(devices[1].SerialNumber);
                    Wrap("ping Esp", () => devices[0].Ping(), () => devices[0].IsOnline.ToStringNull());
                    Wrap("ping Esp_Avr", () => devices[1].Ping(), () => devices[0].IsOnline.ToStringNull());
                    Wrap("time Esp_Avr", () => devices[1].SetTime(DateTime.Now));
                    Wrap("sensors Esp_Avr", () => devices[1].UpdateSensors(), () => "=>" + Extender.JoinToString("; ", devices[1].Sensors.Select(v => v.Value as object)));
                    Wrap("outputs Esp", () => devices[0].SetOut(1, 23));
                    Wrap("outputs Esp_Avr", () => devices[1].SetOut(1, 100));
                    Wrap("sleep Esp", () => devices[0].Sleep(TimeSpan.FromSeconds(10)));
                    Wrap("sleep Esp_Avr", () => devices[1].Sleep(TimeSpan.FromSeconds(40)));

                    devServer.DeviceRepository.Commit();
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
