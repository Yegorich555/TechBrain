using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Extensions
{

    public class DeviceException: Exception
    {        
        public DeviceException(string message): base(message)
        {
        }
    }
}
