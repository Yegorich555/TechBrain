﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Entities
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SensorTypes
    {
        None,
        Temperature,
        Humiditiy,
    }

    public class Sensor : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [DefaultValue(SensorTypes.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public SensorTypes Type { get; set; }
        public SensorValue? Value { get; set; }

        public override string ToString()
        {
            return Id + "-" + Name + ": " + Value?.ToString();
        }
    }
}
