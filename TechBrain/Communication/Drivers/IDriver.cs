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
        int WriteTimeout { get; set; }
        int ReadTimeout { get; set; }
        IDriverClient OpenClient();
    }

    public interface IDriverClient : IDisposable
    {
        void Write(string v);
        void WaitResponse(string v);
        void Write(IEnumerable<byte> bt);
        IList<byte> Read(byte? startByte, byte? endByte, int maxParcelSize = 255);
    }
}
