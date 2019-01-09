using System;
using System.Collections.Generic;

namespace TechBrain.Communication.Protocols
{
    public enum AvrTbCmdType : byte
    {
        setAddress = (byte)'a',
        getAddress = (byte)'b',
        setClock = (byte)'c',
        getSensors = (byte)'g',
        changeOut = (byte)'o',
        changeRepeater = (byte)'t',
        setRepeats = (byte)'u'
    }

    public class TbCommands
    {
        public static IEnumerable<byte> SetClock(DateTime dt)
        {
            int dayOfWeek = (int)dt.DayOfWeek;
            if (dayOfWeek == 0)
                dayOfWeek = 7;
             return SetClock(dayOfWeek, dt.Hour, dt.Minute);
        }

        public static IEnumerable<byte> SetClock(int dayOfWeek, int hours, int minutes)
        {
            yield return (byte)AvrTbCmdType.setClock;
            yield return (byte)dayOfWeek;
            yield return (byte)hours;
            yield return (byte)minutes;

        }

        public static IEnumerable<byte> SetAddress(byte address)
        {
            yield return (byte)AvrTbCmdType.setAddress;
            yield return address;
        }

        public static IEnumerable<byte> ChangeOutput(byte number, byte value)
        {
            yield return (byte)AvrTbCmdType.changeOut;
            yield return number;
            yield return value;
        }

        internal static IEnumerable<byte> GetAddress()
        {
            yield return (byte)AvrTbCmdType.getAddress;
        }

        public static IEnumerable<byte> ChangeRepeater(byte value)
        {
            yield return (byte)AvrTbCmdType.changeRepeater;
            yield return value;
        }

        public static IEnumerable<byte> GetSensors()
        {
            yield return (byte)AvrTbCmdType.getSensors;
        }
    }
}
