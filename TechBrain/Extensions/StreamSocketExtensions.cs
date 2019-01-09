using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static byte[] Read(this NetworkStream stream, byte? startByte = null, byte? endByte = null, int maxParcelSize = 255)
        {
            byte? waitByte = startByte ?? endByte;
            var bytes = new byte[maxParcelSize];
            int i = 0;
            bool canSave = startByte == null;
            var timeout = stream.ReadTimeout;
            if (timeout < 1)
                timeout = 5000;
            var sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (i == maxParcelSize || sw.ElapsedMilliseconds > timeout)
                    throw new TimeoutException("Read timeout: " + timeout + "ms");

                if (waitByte == null)
                {
                    i += stream.Read(bytes, i, bytes.Length - i);
                    break;
                }

                if (!stream.DataAvailable)
                {
                    continue;
                }

                var bt = (byte)stream.ReadByte();
                if (canSave)
                    bytes[i++] = bt;
                if (bt == waitByte)
                {
                    if (i == 0 && startByte != null)
                    { //this is startByte
                        waitByte = endByte;
                        bytes[i++] = bt;
                        canSave = true;
                    }
                    else //this end byte
                        break;
                }
            }
            return bytes.Take(i).ToArray();
        }

        public static byte[] Read(this TcpClient client, byte? startByte = null, byte? endByte = null, int maxParcelSize = 255)
        {
            try
            {
                var stream = client.GetStream();
                return stream.Read(startByte, endByte, maxParcelSize);
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException("TcpClient.Read(...)", ex);
            }

        }

        public static string ReadLine(this TcpClient client)
        {
            var bytes = Read(client, null, (byte)'\n'); //todo find by '\r'
            var str = Encoding.ASCII.GetString(bytes).Trim('\r', '\n');
            return str;
        }

        public static string WaitResponse(this TcpClient client, string waitStr)
        {
            var timeout = client.ReceiveTimeout;
            if (timeout < 1)
                timeout = 5000;
            var sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (sw.ElapsedMilliseconds > timeout)
                    throw new TimeoutException("Read timeout: " + timeout + "ms");

                var str = ReadLine(client);
                if (str.IndexOf(waitStr) != -1)
                    return str;
            }

        }
    }
}
