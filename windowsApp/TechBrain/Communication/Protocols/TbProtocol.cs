﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TechBrain.Communication.Drivers;
using TechBrain.Entities;
using TechBrain.Extensions;

namespace TechBrain.Communication.Protocols
{
    public class TbProtocol : Protocol
    {
        readonly byte _address;
        readonly bool _canAnswer;

        public TbProtocol(IDriver driver, bool canAnswer = true, int address = 0) : base(driver)
        {
            _address = address != 0 ? (byte)address : TbProtocol.DefaultAddr;
            _canAnswer = canAnswer;
        }
        #region Static
        public static byte StartByte { get; set; } = (byte)'>';
        public static byte CommandByte { get; set; } = (byte)'^';
        public static byte EndByte { get; set; } = 250;

        public static byte DefaultAddr { get; set; } = 97;
        public static byte CommonAnswerAddr { get; set; } = 98;
        public static byte BroadcastAddr { get; set; } = 99;

        public static int MinIndexCmdByte { get; set; } = 2;
        public static int ShiftIndexCmd { get; set; } = 4;
        public static int MinParcelSize { get { return MinIndexCmdByte + ShiftIndexCmd + 1; } }
        public static int MaxParcelSize { get; set; } = 30;

        public static int OwnAddress { get; set; } = 1;
        public static int RepeatQuantity { get; set; } = 0;

        public static IEnumerable<byte> GetParcel_ChangeRepeater(int addr, bool isSetRepeater, bool answerEn = true)
        {
            return GetParcel((byte)OwnAddress, (byte)addr, (byte)RepeatQuantity, answerEn, TbCommands.ChangeRepeater((byte)(isSetRepeater ? 1 : 0)));
        }

        public static IEnumerable<byte> GetParcel_ChangeOut(int addr, int number, int value, bool answerEn = true)
        {
            return GetParcel((byte)OwnAddress, (byte)addr, (byte)RepeatQuantity, answerEn, TbCommands.ChangeOutput((byte)number, (byte)value));
        }

        public static IEnumerable<byte> GetParcel_GetSensors(int addr)
        {
            return GetParcel((byte)OwnAddress, (byte)addr, (byte)RepeatQuantity, true, TbCommands.GetSensors());
        }

        public static IEnumerable<byte> GetParcel_GetAddress()
        {
            return GetParcel((byte)OwnAddress, BroadcastAddr, (byte)RepeatQuantity, true, TbCommands.GetAddress());
        }

        public static IEnumerable<byte> GetParcel_SetClock(DateTime dt, bool answerEn = true)
        {
            return GetParcel((byte)OwnAddress, BroadcastAddr, (byte)RepeatQuantity, answerEn, TbCommands.SetClock(dt));
        }

        public static IEnumerable<byte> GetParcel_SetClock(int dayOfWeek, int hours, int minutes, bool answerEn = true)
        {
            return GetParcel((byte)OwnAddress, BroadcastAddr, (byte)RepeatQuantity, answerEn, TbCommands.SetClock(dayOfWeek, hours, minutes));
        }

        public static IEnumerable<byte> GetParcel_SetAddress(int addr, bool forCommonAddr, bool answerEn = true)
        {
            return GetParcel((byte)OwnAddress, forCommonAddr ? BroadcastAddr : DefaultAddr, (byte)RepeatQuantity, answerEn, TbCommands.SetAddress((byte)addr));
        }

        public static byte GetCrc(IEnumerable<byte> str)
        {
            ushort crc = 0;
            foreach (var item in str)
            {
                crc = (ushort)((crc << 3) + item);
                crc = (ushort)((crc << 3) + item);
                crc = (ushort)(crc ^ (crc >> 8));
            }
            byte crcByte = (byte)(crc & 0xFF);
            if (crcByte == EndByte)
                --crcByte;
            return crcByte;
        }

        public static IEnumerable<byte> GetParcel(byte fromAddr, byte toAddr, byte repeatCount, bool answerEnable, IEnumerable<Byte> cmd)
        {
            yield return StartByte;
            var body = GetParcelBody(fromAddr, toAddr, repeatCount, answerEnable, cmd).ToList();
            yield return GetCrc(body);
            foreach (var b in body)
            {
                yield return b;
            }
            yield return EndByte;

        }

        static IEnumerable<byte> GetParcelBody(byte fromAddr, byte toAddr, byte repeatCount, bool answerEnable, IEnumerable<Byte> cmd)
        {
            yield return fromAddr;
            yield return CommandByte;
            yield return toAddr;
            yield return (byte)(repeatCount | (answerEnable ? 128 : 0));
            foreach (var v in cmd)
                yield return v;
        }

        public static IList<byte> FindParcel(IList<byte> parcel, int checkReturnAddr)
        {
            var result = parcel;
            while (true)
            {
                if (IsGoodQuality(result, checkReturnAddr))
                    return result;

                var s = result.IndexOf(StartByte); //find nested parcel
                if (s == -1)
                    return null;

                result = result.Skip(++s).ToList();
            }
        }

        public static bool IsGoodQuality(IList<byte> parcel, int checkReturnAddr, bool checkAnwserAddress = true)
        {
            Trace.WriteLine("TbProtocol. Checking quality...");

            var err = FindError(parcel, checkReturnAddr, checkAnwserAddress);
            if (err != null)
                Trace.WriteLine($"TbProtocol. Bad quality parcel: {err}\n Parcel bytes: {Extender.BuildStringSep(" ", parcel)}");
            else
                Trace.WriteLine("TbProtocol. Checking quality: Successfull");

            return err == null;
        }

        static string FindError(IList<byte> parcel, int checkReturnAddr, bool checkAnwserAddress)
        {
            if (parcel == null || parcel.Count < MinParcelSize)
                return ($"It's small {parcel?.Count.ToStringNull("0")} < {MinParcelSize}");

            var i = parcel.IndexOf(CommandByte);

            if (i < MinIndexCmdByte)
                return ($"IndexOf CommandByte({i}) < MinIndexCmdByte({MinIndexCmdByte})");

            if (!parcel.IndexExist(i + ShiftIndexCmd))
                return ($"It's small after CommandByte. Index {i + ShiftIndexCmd} not exists ({i},{ShiftIndexCmd})");

            if (parcel[i - 1] != checkReturnAddr && checkReturnAddr != BroadcastAddr)
                return ($"Returning address is not match {checkReturnAddr}");

            if (checkAnwserAddress && parcel[i + 1] != CommonAnswerAddr)
                return ($"Returning anwser address is not match {CommonAnswerAddr}");

            var crcParcel = parcel.Skip(i - 1);
            var crc = GetCrc(crcParcel.Take(crcParcel.Count() - 1));
            if (parcel[i - 2] != crc)
                return ($"Crc Error. {parcel[i - 2]} != {crc}");

            return null;
        }

        public static IList<SensorValue> ExtractSensorValues(IList<byte> parcel)
        {
            if (parcel == null)
                return null;
            int i = parcel.IndexOf(CommandByte);
            if (i == -1)
                return null;

            i += ShiftIndexCmd;
            if (!parcel.IndexExist(i + 1))
                return null;

            var lst = new List<SensorValue>();
            for (int k = i; k < (parcel.Count - 1); k += 2)
            {
                lst.Add(SensorValue.From2Bytes(parcel[k], parcel[k + 1]));
            }
            return lst;
        }

        public static byte? ExtractAddressFrom(IList<byte> parcel)
        {
            int i = parcel.IndexOf(CommandByte);
            if (i == -1 || i == 0)
                return null;

            return parcel[i - 1];
        }

        public static void TestCrc(Action<byte, IEnumerable<byte>> afterGetCrc)
        {
            for (byte fromAddr = 1; fromAddr < 5; ++fromAddr)
            {
                for (byte toAddr = 1; toAddr < 50; ++toAddr)
                {
                    for (byte repeatCount = 0; repeatCount < 10; ++repeatCount)
                    {
                        for (byte answerEn = 0; answerEn < 2; ++answerEn)
                        {
                            for (byte setAddr = 0; setAddr < 100; ++setAddr)
                            {
                                Test_GetCrc(afterGetCrc, fromAddr, toAddr, repeatCount, answerEn, TbCommands.SetAddress(setAddr));
                            }
                            for (byte setOut = 1; setOut < 30; ++setOut)
                            {
                                for (byte outputValue = 0; outputValue < 100; ++outputValue)
                                {
                                    Test_GetCrc(afterGetCrc, fromAddr, toAddr, repeatCount, answerEn, TbCommands.ChangeOutput(setOut, outputValue));
                                }
                            }
                            for (byte setD = 1; setD < 8; ++setD)
                            {
                                for (byte setHour = 0; setHour < 24; ++setHour)
                                {
                                    for (byte setMin = 0; setMin < 60; ++setMin)
                                    {
                                        Test_GetCrc(afterGetCrc, fromAddr, toAddr, repeatCount, answerEn, TbCommands.SetClock(setD, setHour, setMin));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static void Test_GetCrc(Action<byte, IEnumerable<byte>> afterGetCrc, byte fromAddr, byte toAddr, byte repeatCount, byte answerEnable, IEnumerable<byte> cmd)
        {
            var parcel = TbProtocol.GetParcel(fromAddr, toAddr, repeatCount, answerEnable == 1, cmd);
            var crc = GetCrc(parcel);
            afterGetCrc?.Invoke(crc, parcel);
        }

        public static IEnumerable<byte> GetResponse(IList<byte> bytes)
        {
            if (!IsGoodQuality(bytes, BroadcastAddr, false))
                return new byte[0];
            var i = bytes.IndexOf(CommandByte);
            var b = (AvrTbCmdType)bytes[i + 3];


            IEnumerable<byte> GetAnswer()
            {
                var lst = new List<byte>()
                {
                    (byte)AvrTbCmdType.sendAnswer
                };


                Trace.WriteLine($"TbProtocol.GetResponse for {b.ToString()} command...");
                bool isOk = false;
                switch (b)
                {
                    case AvrTbCmdType.getAddress:
                    case AvrTbCmdType.setClock:
                    case AvrTbCmdType.changeOut:
                        isOk = true;
                        break;
                    case AvrTbCmdType.getSensors:
                        void addSensor(int value)
                        {
                            lst.Add((byte)(value >> 8));
                            lst.Add((byte)(value & 0xFF));
                        }
                        addSensor(105); //10.5
                        addSensor(231);
                        addSensor(124);
                        addSensor(549);
                        break;

                }

                if (lst.Count > 1 || isOk)
                {
                    lst.AddRange(Encoding.ASCII.GetBytes("OK"));
                    return lst;
                }
                throw new NotImplementedException();
            }

            var answer = GetAnswer();
            //var toAddr = ExtractAddressFrom(bytes);
            var fromAddr = bytes[i + 1];
            var bt = GetParcel(fromAddr, CommonAnswerAddr, 1, true, answer);

            //fix repeatCountPart
            //var i2 = bytes.IndexOf(CommandByte);
            //bt[i2 + 2] = bytes[i + 2];

            return bt;
        }

        #endregion

        T WaitResponse<T>(IDriverClient client, Func<IList<byte>, T> extractFunc, int addr = 0)
        {
            while (true)
            {
                var parcel = client.Read(TbProtocol.StartByte, TbProtocol.EndByte, TbProtocol.MaxParcelSize);
                var result = TbProtocol.FindParcel(parcel, addr == 0 ? _address : addr);
                if (result != null)
                    return extractFunc(parcel);
            }
        }

        public override bool Ping()
        {
            try
            {
                using (var client = Driver.OpenClient())
                {
                    var bt = TbProtocol.GetParcel_GetAddress();
                    client.Write(bt);
                    var addr = WaitResponse(client, TbProtocol.ExtractAddressFrom);
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                Trace.WriteLine(ex);
                return false;
            }
        }

        public override void SetTime(DateTime dt)
        {
            using (var client = Driver.OpenClient())
            {
                var bt = TbProtocol.GetParcel_SetClock(dt, _canAnswer);
                client.Write(bt);
                if (!_canAnswer)
                    return;
                var addr = WaitResponse(client, TbProtocol.ExtractAddressFrom);
            }
        }

        public override void UpdateSensors(IList<Sensor> sensors)
        {
            using (var client = Driver.OpenClient())
            {
                var bt = TbProtocol.GetParcel_GetSensors(_address);
                client.Write(bt);
                var values = WaitResponse(client, TbProtocol.ExtractSensorValues);
                var count = Math.Min(values.Count, sensors.Count);
                for (int i = 0; i < count; ++i)
                    sensors[i].Value = values[i];
            }
        }

        public override void SetOut(int number, int value)
        {
            if (number < 0 || number > 10)
                throw new ArgumentOutOfRangeException("number", $"Number == {number} must be 0..10");

            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException("value", $"Value == {value} must be 0..100");

            using (var client = Driver.OpenClient())
            {
                var bt = TbProtocol.GetParcel_ChangeOut(_address, number, value, _canAnswer);
                client.Write(bt);
                if (!_canAnswer)
                    return;
                var addr = WaitResponse(client, TbProtocol.ExtractAddressFrom);
                if (addr != _address)
                    throw new InvalidOperationException("Address mismatches {addr} != {address}");

                return;
            }
        }

        public override void Sleep(TimeSpan time)
        {
            throw new NotSupportedException();
        }
    }
}
