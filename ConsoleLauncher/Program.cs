using System;
using System.Diagnostics;
using TechBrain.CustomEventArgs;
using TechBrain.Extensions;
using TechBrain.Services;

namespace ConsoleLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var devServer = DevServer.Instance;
            devServer.ErrorAppeared += DevServer_ErrorAppeared;
            while (true)
            {
                string txt = null;
                try
                {
                    var line = Console.ReadLine();
                    if (line.ContainsText("syncTime"))
                    {
                        devServer.SyncTime();
                        txt = "Ok";
                    }
                    else if (line.ContainsText("getAddress"))
                    {
                        var str = devServer.GetAddress();
                        txt = Extender.BuildString("Got address: ", str);
                    }
                    else if (line.ContainsText("getSensors")) //getSenors(addr)
                    {
                        var addr = Convert.ToInt32(line.Extract('(', ')'));
                        var lst = devServer.GetSensorsValues(addr);
                        txt = Extender.BuildString("Sensors: ", lst);
                    }
                    else if (line.ContainsText("setTime")) //setTime(hh,mm,weekDay)
                    {
                        var val = line.Extract('(', ')');
                        var ind1 = line.IndexOf('(') + 1;
                        var ind2 = line.IndexOf(',');
                        var hhStr = line.Substring(ind1, ind2 - ind1);
                        var ind3 = line.IndexOf(',', ++ind2);
                        var mmStr = line.Substring(ind2, ind3 - ind2);
                        var dwStr = line.Substring(++ind3, 1);

                        var hh = Convert.ToInt32(hhStr);
                        var mm = Convert.ToInt32(mmStr);
                        var dw = Convert.ToInt32(dwStr);
                        var str = devServer.SetTime(hh, mm, dw);
                        txt = Extender.BuildString("SetTime hh:mm:dayWeek => ", hh, ":", mm, "/", dw, " - Ok");
                    }
                    else if (line.ContainsText("setAddrress"))
                    {
                        var addr = Convert.ToInt32(line.Extract('(', ')'));
                        devServer.SetAddress(addr);
                        txt = Extender.BuildString("Setted Address: ", addr);
                    }
                    else
                        txt = "Is not recognized";

                    if (txt != null)
                    {
                        Console.WriteLine(txt);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            }
        }

        private static void DevServer_ErrorAppeared(object sender, CommonEventArgs e)
        {
            Debug.WriteLine(e.Message);
            Console.WriteLine(e.Message);
        }
    }
}
