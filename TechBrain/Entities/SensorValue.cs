using System;
using System.Globalization;
using TechBrain.Extensions;
using TechBrain.Services;

namespace TechBrain.Entities
{
    public class SensorValue
    {
        public static SensorValue From2Bytes(byte highByte, byte lowByte)
        {
            return new SensorValue((highByte << 8) | lowByte);
        }

        private SensorValue() : this(0, DateTime.MinValue)
        { }

        public SensorValue(int sourceValue, DateTime dt, int divider = 10)
        {
            SourceValue = sourceValue;
            DateTime = dt;
            Divider = 10;
        }

        public SensorValue(int sourceValue, int divider = 10) : this(sourceValue, DateTimeService.Now, divider)
        { }

        public int? Divider { get; private set; } = 10;// for x dig after point
        public float? Value { get { return SourceValue / Divider; } } 
        public int? SourceValue { get; private set; }
        public DateTime DateTime { get; private set; }

        public override string ToString()
        {
            if (SourceValue == null)
                return "null";
            return Extender.BuildString(Value);
        }
    }
}
