using System.Collections.Generic;
using System.Net.Sockets;
using TechBrain.Extensions;

namespace TechBrain.Communication.Drivers
{
    public class TcpDriverClient : IDriverClient
    {
        private TcpClient client;

        public TcpDriverClient(TcpClient client)
        {
            this.client = client;
        }

        public void Dispose() => client.Dispose();

        public IList<byte> Read(byte startByte, byte endByte, int maxParcelSize)
        {
            throw new System.NotImplementedException();
        }

        public void WaitResponse(string v) => client.WaitResponse(v);
        public void Write(string v) => client.Write(v);

        public void Write(IEnumerable<byte> bt)
        {
            throw new System.NotImplementedException();
        }
    }
}
