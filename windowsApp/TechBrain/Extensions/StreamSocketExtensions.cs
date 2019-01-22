using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TechBrain.Extensions
{
    public static class StreamSocketExtensions
    {
        public static void Write(this NetworkStream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this TcpClient client, string str)
        {
            Write(client, Encoding.ASCII.GetBytes(str));
        }
        public static void Write(this TcpClient client, IEnumerable<byte> bytes)
        {
            client.Client.Send(bytes.ToArray());
        }

        static byte[] BaseRead(this NetworkStream stream, byte? startByte, byte? endByte, byte? endByte2, int maxParcelSize = 255)
        {
            byte? waitByte = startByte ?? endByte ?? endByte2;
            byte? waitByte2 = startByte == null ? endByte2 : null;
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
                if (bt == waitByte || bt == waitByte2)
                {
                    if (i == 0 && startByte != null)  //this is startByte
                    {
                        waitByte = endByte ?? endByte2;
                        waitByte2 = endByte2;
                        bytes[i++] = bt;
                        canSave = true;
                    }
                    else //this end byte
                        break;
                }
            }
            Debug.WriteLine("TcpClient ReadTime: " + sw.ElapsedMilliseconds);

            return bytes.Take(i).ToArray();
        }

        public static byte[] Read(this NetworkStream stream, byte? startByte = null, byte? endByte = null, int maxParcelSize = 255)
        {
            return BaseRead(stream, startByte, endByte, null, maxParcelSize);
        }

        public static byte[] Read(this TcpClient client, byte? startByte = null, byte? endByte = null, int maxParcelSize = 255)
        {
            try
            {
                var stream = client.GetStream();
                return Read(stream, startByte, endByte, maxParcelSize);
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException("TcpClient.Read(...)", ex);
            }

        }

        public static string ReadLine(this TcpClient client)
        {
            var bytes = BaseRead(client.GetStream(), null, (byte)'\n', (byte)'\r');
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
                {
                    Debug.WriteLine("TcpClient ReadTime: " + sw.ElapsedMilliseconds);
                    return str;
                }
            }

        }
    }
}
