using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TechBrain;
using TechBrain.Extensions;
using TechBrain.Services;

namespace ConsoleTest
{
    class Program
    {
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
                Console.Title = "Simulator_TechBrain";
                Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

                var config = Config.ReadFromFile();
                var devServer = new DevServer(config);
                if (devServer.DeviceRepository.Count == 0)
                    devServer = new DevServer(config, Simulator.GenerateNewDevices(config));
                devServer.Start();

                var devices = devServer.DeviceRepository.GetAll();
                var sim = new Simulator(config, devices);
                sim.Start();
                foreach (var dev in devices)
                    dev.IpPort = config.Esp_TcpPort;

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
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }
}
