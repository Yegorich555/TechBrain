using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TechBrain.Entities
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum OutputTypes
    {
        None,
        Digital,
        Pwm,
    }

    public class DeviceOutput: IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public OutputTypes Type { get; set; }
        public int? Value { get; internal set; }

        public override string ToString()
        {
            return Id + "-" + Name + ": " + Value?.ToString();
        }
    }
}
