using Newtonsoft.Json;
using System;
using TechBrain.Extensions;
using TechBrain.Services;

namespace TechBrain.Entities
{
    [JsonConverter(typeof(SensorValueConverter))]
    public struct SensorValue
    {
        public const int Divider = 10;

        public SensorValue(int sourceValue) => SourceValue = sourceValue;
        public SensorValue(double value) => SourceValue = Convert.ToInt32(value * Divider);
        public SensorValue(string value)
        {
            var arr = value.Split('.', ',');
            var v = Convert.ToInt32(arr[0]) * Divider;
            if (arr.Length == 2)
            {
                if (arr[1].Length > Divider / 10)
                    throw new InvalidCastException("Invalid parse SensorValue from string");
                v += Convert.ToInt32(arr[1]);
            }

            SourceValue = v;
        }
        internal int SourceValue { get; set; }

        public static SensorValue From2Bytes(byte highByte, byte lowByte) => new SensorValue((short)((highByte << 8) | lowByte));

        public override string ToString()
        {
            return Extender.BuildStringSep(".", SourceValue / Divider, SourceValue % Divider);
        }

        public static implicit operator int(SensorValue v) => v.SourceValue;
    }

    public class SensorValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            if (objectType == typeof(string))
                return new SensorValue((string)reader.Value);
            else
                return new SensorValue(Convert.ToDouble(reader.Value));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            float? v = value == null ? null : (float?)((SensorValue)value).SourceValue / SensorValue.Divider;
            writer.WriteValue(v);
        }
    }
}
