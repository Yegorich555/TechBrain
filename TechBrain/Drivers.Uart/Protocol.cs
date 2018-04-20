using System;
using System.Collections.Generic;
using System.Linq;
using TechBrain.Entities;
using TechBrain.Extensions;
using TechBrain.Services.FLogger;

namespace TechBrain.Drivers.Uart
{
    public class Protocol
    {
        public static byte StartByte { get; set; } = (byte)'>';
        public static byte CommandByte { get; set; } = (byte)'^';
        public static byte EndByte { get; set; } = 250;

        public static byte DefaultAddr { get; set; } = 97;
        public static byte CommonAnswerAddr { get; set; } = 98;
        public static byte CommonAddr { get; set; } = 99;

        public static int MinIndexCmdByte { get; set; } = 2;
        public static int ShiftIndexCmd { get; set; } = 4;
        public static int MinParcelSize { get { return MinIndexCmdByte + ShiftIndexCmd + 1; } }
        public static int MaxParcelSize { get; set; } = 30;

        public static int OwnAddress { get; set; } = 1;
        public static int RepeatQuantity { get; set; } = 4;

        public static IEnumerable<byte> GetParcel_ChangeRepeater(int addr, bool isSetRepeater, bool answerEn = true)
        {
            return GetParcelCmd((byte)OwnAddress, (byte)addr, (byte)RepeatQuantity, answerEn, Command.ChangeRepeater((byte)(isSetRepeater ? 1 : 0)));
        }

        public static IEnumerable<byte> GetParcel_ChangeOut(int addr, int number, int value, bool answerEn = true)
        {
            return GetParcelCmd((byte)OwnAddress, (byte)addr, (byte)RepeatQuantity, answerEn, Command.ChangeOutput((byte)number, (byte)value));
        }

        public static IEnumerable<byte> GetParcel_GetSensors(int addr, bool answerEn = true)
        {
            return GetParcelCmd((byte)OwnAddress, (byte)addr, (byte)RepeatQuantity, answerEn, Command.GetSensors());
        }

        public static IEnumerable<byte> GetParcel_GetAddress(bool answerEn = true)
        {
            return GetParcelCmd((byte)OwnAddress, CommonAddr, (byte)RepeatQuantity, answerEn, Command.GetAddress());
        }

        public static IEnumerable<byte> GetParcel_SetClock(DateTime dt, bool answerEn = true)
        {
            return GetParcelCmd((byte)OwnAddress, CommonAddr, (byte)RepeatQuantity, answerEn, Command.SetClock(dt));
        }

        public static IEnumerable<byte> GetParcel_SetClock(int dayOfWeek, int hours, int minutes, bool answerEn = true)
        {
            return GetParcelCmd((byte)OwnAddress, CommonAddr, (byte)RepeatQuantity, answerEn, Command.SetClock(dayOfWeek, hours, minutes));
        }


        public static IEnumerable<byte> GetParcel_SetAddress(int addr, bool forCommonAddr, bool answerEn = true)
        {
            return GetParcelCmd((byte)OwnAddress, forCommonAddr ? CommonAddr : DefaultAddr, (byte)RepeatQuantity, answerEn, Command.SetAddress((byte)addr));
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

        public static IEnumerable<byte> GetParcelCmd(byte fromAddr, byte toAddr, byte repeatCount, bool answerEnable, IEnumerable<Byte> cmd)
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

                var s = result.IndexOf(StartByte);
                if (s == -1)
                    return null;

                result = result.Skip(++s).ToList();
            }
        }

        public static bool IsGoodQuality(IList<byte> parcel, int checkReturnAddr)
        {
            Logger.Debug("Uart.Protocol. Checking quality...");

            var err = FindError(parcel, checkReturnAddr);
            if (err != null)
                Logger.Debug($"Uart.Protocol. Bad quality parcel: {err}\n Parcel bytes: {Extender.BuildStringSep(" ", parcel)}");
            else
                Logger.Debug("Uart.Protocol. Checking quality successfull");

            return err == null;
        }

        static string FindError(IList<byte> parcel, int checkReturnAddr)
        {

            if (parcel == null || parcel.Count() < MinParcelSize)
                return ($"It's small {parcel?.Count().ToStringNull("0")} < {MinParcelSize}");

            var i = parcel.IndexOf(CommandByte);

            if (i < MinIndexCmdByte)
                return ($"IndexOf CommandByte({i}) < MinIndexCmdByte({MinIndexCmdByte})");

            if (!parcel.IndexExist(i + ShiftIndexCmd))
                return ($"It's small after CommandByte. Index {i + ShiftIndexCmd} not exists ({i},{ShiftIndexCmd})");

            if (parcel[i - 1] != checkReturnAddr)
                return ($"Returning address is not match {checkReturnAddr}");

            var crc = GetCrc(parcel.Skip(i - 1));
            if (parcel[i - 2] != crc)
                return ($"Crc Error. {parcel[i - 2]} != {crc}");

            return null;
        }

        public static IEnumerable<SensorValue> ExtractSensorValues(IList<byte> parcel)
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

        public static byte? ExtractAddress(IList<byte> parcel)
        {
            int i = parcel.IndexOf(CommandByte);
            if (i == -1)
                return null;

            if (parcel.TryGetValue(i + ShiftIndexCmd, out byte v))
                return v;
            return null;
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
                                Test_GetCrc(afterGetCrc, fromAddr, toAddr, repeatCount, answerEn, Command.SetAddress(setAddr));
                            }
                            for (byte setOut = 1; setOut < 30; ++setOut)
                            {
                                for (byte outputValue = 0; outputValue < 100; ++outputValue)
                                {
                                    Test_GetCrc(afterGetCrc, fromAddr, toAddr, repeatCount, answerEn, Command.ChangeOutput(setOut, outputValue));
                                }
                            }
                            for (byte setD = 1; setD < 8; ++setD)
                            {
                                for (byte setHour = 0; setHour < 24; ++setHour)
                                {
                                    for (byte setMin = 0; setMin < 60; ++setMin)
                                    {
                                        Test_GetCrc(afterGetCrc, fromAddr, toAddr, repeatCount, answerEn, Command.SetClock(setD, setHour, setMin));
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
            var parcel = Protocol.GetParcelCmd(fromAddr, toAddr, repeatCount, answerEnable == 1, cmd);
            var crc = GetCrc(parcel);
            afterGetCrc?.Invoke(crc, parcel);
        }
    }
}
