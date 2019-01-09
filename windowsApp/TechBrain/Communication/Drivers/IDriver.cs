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
        int ResponseTimeout { get; set; }
        IDriverClient OpenClient();
    }

    public interface IDriverClient : IDisposable
    {
        void Write(string v);
        void Write(IEnumerable<byte> bt);
        string WaitResponse(string v);
        IList<byte> Read(byte? startByte, byte? endByte, int maxParcelSize = 255);
    }
}
