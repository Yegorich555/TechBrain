using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Communication.Drivers
{
    public interface IDriverClient : IDisposable
    {
        void Write(string v);
        void WaitResponse(string v);
        void Write(IEnumerable<byte> bt);
        IList<byte> Read(byte startByte, byte endByte, int maxParcelSize);
    }
}
