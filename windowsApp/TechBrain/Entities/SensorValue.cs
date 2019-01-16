using Newtonsoft.Json;
using System;
using TechBrain.Extensions;
using TechBrain.Services;

namespace TechBrain.Entities
{
    [JsonConverter(typeof(SensorValueConverter))]
    public class SensorValue
    {
        public static SensorValue From2Bytes(byte highByte, byte lowByte)
        {
            return new SensorValue((short)((highByte << 8) | lowByte));
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

        public SensorValue(float value) : this(Convert.ToInt16(value * 10))
        { }

        public int? Divider { get; private set; } = 10;// for x dig after point
        public float? Value { get { return (float?)SourceValue / Divider; } }
        public int? SourceValue { get; private set; }
        public DateTime DateTime { get; private set; }

        public override string ToString()
        {
            if (SourceValue == null)
                return "null";
            return Extender.BuildStringSep(".", (int)SourceValue / Divider, SourceValue % Divider);
        }
    }

    public class SensorValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(float));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new SensorValue((float)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value ?? ((SensorValue)value).Value); //todo optimize
        }
    }
}
