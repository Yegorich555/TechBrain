using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Communication.Drivers
{
    enum DriverTypes
    {
        COM,
        ESP
    }

    public interface IDriver
    {
        //int WriteReadTimeout { get; set; }
        //bool Write(IEnumerable<byte> bytes);
        //IList<byte> Read(byte? startWaitByte = null, byte? endWaitByte = null, int maxLength = 255);
        //T WriteRead<T>(int addr, IEnumerable<byte> bt, Func<IList<byte>, T> extractFunc);
        //IDisposable GetClient();
        IDriverClient OpenClient();
    }
}
