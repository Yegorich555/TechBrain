using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TechBrain.Extensions
{
    public static class StreamSocketExtensions
    {
        public static void Write(this TcpClient client, string str)
        {
            Write(client, Encoding.ASCII.GetBytes(str));
        }
        public static void Write(this TcpClient client, IEnumerable<byte> bytes)
        {
            client.Client.Send(bytes.ToArray());
        }

        public static void WaitResponse(this TcpClient client, string waitStr)
        {
            using (var stream = client.GetStream())
            {
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                {
                    while (true)
                    {
                        var str = reader.ReadLine();
                        if (str.IndexOf(waitStr) != -1)
                            return;
                    }
                }
            }
        }
    }
}
