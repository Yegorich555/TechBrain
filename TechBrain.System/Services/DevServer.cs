using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechBrain.Entities;
using TechBrain.Extensions;
using TechBrain.IO;
using TechBrain.Drivers.Uart;
using TechBrain.Services;

namespace TechBrain.Services
{
    public class DevServer : IError
    {
        public ComPort ComPort { get; set; } = new ComPort();

        private static DevServer instance;
        public static DevServer Instance
        {
            get
            {
                if (instance == null)
                    instance = new DevServer();
                return instance;
            }
        }

        private DevServer()
        {
            ComPort.RepeatQuantity = 2;
            ComPort.BaudRate = BaudRate._4800;
            ComPort.PortName = "com3";
            ComPort.ErrorAppeared += (sender, args) => OnErrorAppeared(args);
        }

        object objLock = new object();
        public bool SyncTime()
        {
            lock (objLock)
            {
                try
                {
                    var bt = Protocol.GetParcel_SetClock(DateTime.Now);
                    return ComPort.Write(bt);
                }
                catch (Exception ex) { OnErrorAppeared(ex); }
                return false;
            }
        }

        public bool SetTime(int dayOfWeek, int hours, int minutes)
        {
            lock (objLock)
            {
                try
                {
                    var bt = Protocol.GetParcel_SetClock(dayOfWeek, hours, minutes);
                    return ComPort.Write(bt);
                }
                catch (Exception ex) { OnErrorAppeared(ex); }
                return false;
            }
        }

        public int? GetAddress()
        {
            try
            {
                var bt = Protocol.GetParcel_GetAddress();
                var v = (int?)WriteRead(Protocol.CommonAddr, bt, Protocol.ExtractAddress);
                return v;
            }
            catch (Exception ex) { OnErrorAppeared(ex); }
            return null;
        }

        public IEnumerable<SensorValue> GetSensorsValues(int addr)
        {
            try
            {
                var bt = Protocol.GetParcel_GetSensors(addr);
                IEnumerable<SensorValue> v = WriteRead(addr, bt, Protocol.ExtractSensorValues);
                return v;
            }
            catch (Exception ex) { OnErrorAppeared(ex); }
            return null;
        }

        object wrLock = new object();
        private T WriteRead<T>(int addr, IEnumerable<byte> bt, Func<IList<byte>, T> extractFunc)
        {
            lock (wrLock)
            {
                ComPort.RepeatQuantity = Protocol.RepeatQuantity;

                if (!ComPort.Write(bt))
                    return default(T);
                var sw = new Stopwatch();
                sw.Start();
                while (true)
                {
                    if (sw.ElapsedMilliseconds > ComPort.ReceiveTimeout * 2)
                        return default(T);

                    var parcel = ComPort.Read(Protocol.StartByte, Protocol.EndByte, Protocol.MaxParcelSize);
                    if (!parcel.Any())
                        return default(T);

                    var result = Protocol.FindParcel(parcel, addr);
                    if (result != null)
                        return extractFunc(parcel);
                }
            }
        }

        public void SetAddress(int addr, bool forCommonAddr = false)
        {
            try
            {
                var bt = Protocol.GetParcel_SetAddress(addr, forCommonAddr);
                ComPort.Write(bt);
                //todo wait answer
            }
            catch (Exception ex) { OnErrorAppeared(ex); }
        }

        public void ChangeOut(int addr, int number, int value)
        {
            try
            {
                var bt = Protocol.GetParcel_ChangeOut(addr, number, value);
                ComPort.Write(bt);
                //todo wait answer
            }
            catch (Exception ex) { OnErrorAppeared(ex); }
        }

        public void ChangeRepeater(int addr, bool isSetRepeater)
        {
            try
            {
                var bt = Protocol.GetParcel_ChangeRepeater(addr, isSetRepeater);
                ComPort.Write(bt);
                //todo wait answer
            }
            catch (Exception ex) { OnErrorAppeared(ex); }
        }

    }


}

