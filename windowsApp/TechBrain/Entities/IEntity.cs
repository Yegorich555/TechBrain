﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Entities
{
    public interface IEntity
    {        
        int SerialNumber { get; set; }
        string Name { get; set; }
        string Description { get; set; }
    }
}