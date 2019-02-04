using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using TechBrain.CustomEventArgs;
using TechBrain.Extensions;
using TechBrain.Services.FLogger;

namespace TechBrain.IO
{
    //SerialPort doesn't exist in .net core
    //public class ComPort : IError
    //{
    //    public event EventHandler<ComPortReceiveArgs> BytesReceived;

    //    SerialPort port = new SerialPort();
    //    //SimulSerialPort port = new SimulSerialPort();

    //    public ComPort(string portName, BaudRate baudRate, DataBits dataBits, Parity parity, StopBits stopBits) : this()
    //    {
    //        PortName = portName;
    //        BaudRate = baudRate;
    //        DataBits = dataBits;
    //        Parity = parity;
    //        StopBits = stopBits;
    //    }

    //    public ComPort()
    //    { }

    //    public void DiscardInBuffer()
    //    {
    //        port.DiscardInBuffer();
    //    }

    //    object writeLock = new object();
    //    public bool Write(IEnumerable<byte> bt)
    //    {
    //        try
    //        {
    //            if (bt == null || !bt.Any())
    //                return false;
    //            lock (writeLock)
    //            {
    //                if (!LoopUntilOpen(OpenTimeout))
    //                {
    //                    return false;
    //                }

    //                if (SendWithDiscardInBuffer)
    //                    port.DiscardInBuffer();

    //                var arr = bt.ToArray();
    //                Logger.Debug($"ComPort.{PortName}.Writing : {Extender.BuildStringSep(" ", arr)}");
    //                for (int i = 0; i < RepeatQuantity; ++i)
    //                {
    //                    port.Write(arr, 0, arr.Length);
    //                }
    //            }
    //            return true;
    //        }
    //        catch (Exception ex) { OnErrorAppeared(ex); }
    //        return false;

    //    }

    //    object readLock = new object();
    //    public IList<byte> Read(byte? startWaitByte = null, byte? endWaitByte = null, int maxLength = 255)
    //    {
    //        lock (readLock)
    //        {
    //            var bytes = GoRead(startWaitByte, endWaitByte, maxLength);
    //            var empty = bytes == null || bytes.Count < 0;

    //            var result = empty ? "is null" : Extender.BuildStringSep(" ", bytes);
    //            Logger.Debug($"ComPort.{PortName}.Readed: {result}");

    //            if (empty)
    //                BytesReceived?.Invoke(this, new ComPortReceiveArgs(bytes));
    //            return bytes;
    //        }
    //    }

    //    IList<byte> GoRead(byte? startWaitByte, byte? endWaitByte, int maxLength)
    //    {
    //        try
    //        {
    //            bool goAdd = startWaitByte == null;
    //            var bytes = new List<byte>();
    //            Stopwatch sw = null;
    //            while (true)
    //            {
    //                Thread.Sleep(1);
    //                if (!LoopUntilOpen(OpenTimeout))
    //                {
    //                    return bytes;
    //                }
    //                if (sw == null)
    //                {
    //                    sw = new Stopwatch();
    //                    sw.Start();
    //                }

    //                var cnt = 0;
    //                try { cnt = port.BytesToRead; } catch { }

    //                if (sw.ElapsedMilliseconds >= ReceiveTimeout)
    //                {
    //                    OnErrorAppeared("Error timeout receiving of ComPort ", PortName, "; ", ReceiveTimeout + "ms");
    //                    return bytes;
    //                }

    //                for (int i = 0; i < cnt; ++i)
    //                {
    //                    var b = port.ReadByte();

    //                    if (goAdd)
    //                    {
    //                        bytes.Add((byte)b);

    //                        if (b == endWaitByte)
    //                        {
    //                            return bytes;
    //                        }                           

    //                        //check max length and extract new from receiving if we need
    //                        if (bytes.Count >= maxLength)
    //                        {
    //                            if (endWaitByte == null)
    //                            {
    //                                return bytes;
    //                            }

    //                            var needClear = true;
    //                            if (startWaitByte != null)
    //                            {
    //                                var s = bytes.IndexOf((byte)startWaitByte); //find new start parcel
    //                                if (s != -1)
    //                                {
    //                                    needClear = false;
    //                                    bytes = bytes.GetRangeByIndex(s, bytes.Count - 1);
    //                                }
    //                            }

    //                            if (needClear)
    //                            {
    //                                bytes.Clear();
    //                                if (startWaitByte != null)
    //                                    goAdd = false;
    //                            }
    //                            continue;
    //                        }
    //                    }
    //                    else if (b == startWaitByte)
    //                    {
    //                        goAdd = true;
    //                        bytes.Add((byte)b);
    //                    }
    //                }

    //                if (endWaitByte == null && goAdd)
    //                {
    //                    return bytes;
    //                }
    //            }
    //        }
    //        catch (Exception ex) { OnErrorAppeared(ex); }

    //        return null;
    //    }

    //    public void Open()
    //    {
    //        if (!port.IsOpen)
    //        {
    //            GoConfigPort();
    //        }
    //        port.Open();
    //    }

    //    public void Close()
    //    {
    //        port.Close();
    //    }


    //    public bool LoopUntilOpen(int timeOutValue = -1) //todo timeWait
    //    {
    //        if (port.IsOpen)
    //            return true;
    //        if (timeOutValue == 0)
    //        {
    //            OnErrorAppeared("Error timeout opening of ComPort ", PortName, "; ", OpenTimeout + "ms");
    //            return false;
    //        }
    //        try
    //        {
    //            var sw = new Stopwatch();
    //            sw.Start();

    //            while (!port.IsOpen)
    //            {
    //                if (timeOutValue > -1 && sw.ElapsedMilliseconds > timeOutValue)
    //                {
    //                    OnErrorAppeared("Error timeout opening of ComPort ", PortName, "; ", OpenTimeout + "ms");
    //                    return false;
    //                }
    //                if (PortOwnExist())
    //                {
    //                    Thread.Sleep(1000);
    //                    try { Open(); Thread.Sleep(300); } catch (IOException) { }
    //                }
    //                else Thread.Sleep(10);
    //            }
    //            sw.Stop();
    //            return true;
    //        }
    //        catch (Exception ex) { OnErrorAppeared(ex); }

    //        return false;
    //    }

    //    public static bool PortExist(string portName)
    //    {
    //        var portNames = SerialPort.GetPortNames();
    //        return portNames.Contains(portName, StringComparer.OrdinalIgnoreCase);
    //    }

    //    public bool PortOwnExist()
    //    {
    //        var portNames = SerialPort.GetPortNames();
    //        return portNames.Contains(PortName, StringComparer.OrdinalIgnoreCase);
    //    }

    //    #region Properties
    //    public bool DiscardNull
    //    {
    //        get
    //        {
    //            return port.DiscardNull;
    //        }
    //        set
    //        {
    //            port.DiscardNull = value;
    //        }
    //    }

    //    public bool SendWithDiscardInBuffer { get; set; } = true;
    //    /// <summary>
    //    /// Timeout for opening port (ms)
    //    /// </summary>
    //    public int OpenTimeout { get; set; } = 1000;

    //    /// <summary>
    //    /// Timeout for waiting receive (ms)
    //    /// </summary>
    //    public int ReceiveTimeout { get; set; } = 2000; //ms

    //    /// <summary>
    //    /// Quantity repeats in one transiving
    //    /// </summary>
    //    public int RepeatQuantity { get; set; } = 1;

    //    public string PortName { get; set; } = "COM1";
    //    public BaudRate BaudRate { get; set; } = BaudRate._9600;
    //    public DataBits DataBits { get; set; } = DataBits.b8;
    //    public Parity Parity { get; set; } = Parity.None;
    //    public StopBits StopBits { get; set; } = StopBits.One;

    //    /// <summary>
    //    ///Accept change for realTime properties and reOpen port if it worked
    //    /// </summary>
    //    public void AcceptChangeProperties()
    //    {
    //        var open = port.IsOpen;
    //        if (open)
    //            port.Close();

    //        GoConfigPort();

    //        if (open)
    //            port.Open();
    //    }

    //    void GoConfigPort()
    //    {
    //        try
    //        {
    //            port.PortName = PortName;
    //            port.BaudRate = (int)BaudRate;
    //            port.DataBits = (int)DataBits;
    //            port.Parity = Parity;
    //            port.StopBits = StopBits;
    //        }
    //        catch (Exception ex)
    //        {
    //            OnErrorAppeared(ex);
    //        }

    //    }
    //    #endregion
    //}


    //public enum BaudRate
    //{
    //    _2400 = 2400, _4800 = 4800, _9600 = 9600, _14400 = 14400, _19200 = 19200,
    //    _28800 = 28800, _38400 = 38400, _57600 = 57600, _76800 = 76800, _115200 = 115200, _230400 = 230400
    //}

    //public enum DataBits { b5 = 5, b6 = 6, b7 = 7, b8 = 8 }
}