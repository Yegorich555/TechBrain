using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace TechBrain.Entities
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OutputTypes
    {
        None,
        Digital,
        Pwm,
    }

    public class Output : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [DefaultValue(OutputTypes.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public OutputTypes Type { get; set; }

        [SaveIgnore]
        public int? Value { get; internal set; }

        public override string ToString()
        {
            return Id + "-" + Name + ": " + Value?.ToString();
        }
    }
}
