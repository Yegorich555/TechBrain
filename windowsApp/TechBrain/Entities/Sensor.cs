﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Entities
{
    public enum SensorTypes
    {
        None,
        Temperature,
        Humiditiy,
    }

    public class Sensor : IEntity
    {
        public int SerialNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public SensorTypes Type { get; set; }
        public SensorValue Value { get; set; }

        //todo min and maxValue

        public override string ToString()
        {
            return SerialNumber + "-" + Name + ": " + Value?.ToString();
        }
    }
}