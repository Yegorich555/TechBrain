using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TechBrain.Services
{
    public class TcpServer
    {
        public event EventHandler<string> ErrorLog;
        public event EventHandler<TcpClient> GotNewClient;

        public string ThreadName { get; set; } = "TcpServer";
        public int Port { get; set; } = 555;
        public int ReceiveTimeout { get; set; } = 5000;
        public int SendTimeout { get; set; } = 5000;
        public bool TryAnotherPorts { get; set; } = true;

        private volatile bool _runThread;
        private object _runThreadLock = new object();
        Thread _thread;

        public void Start()
        {
            Listen();
        }

        void Listen()
        {
            _runThread = true;
            _thread = new Thread(() =>
            {
                bool _localRunThread = true;

                TcpListener server = null;
                var port = Port;
                
                while (_localRunThread)
                {
                    lock (_runThreadLock)
                    {
                        _localRunThread = _runThread;
                    }
                    try
                    {
                        server = new TcpListener(IPAddress.Any, port++);
                        server.Start();
                        Trace.WriteLine($"TcpServer '{ThreadName}' started on " + server.LocalEndpoint);
                        break;
                    }
                    catch (SocketException)
                    {
                        if (!TryAnotherPorts)
                            throw;
                    }
                }

                while (_localRunThread)
                {
                    lock (_runThreadLock)
                    {
                        _localRunThread = _runThread;
                    }
                    try
                    {
                        if (!server.Pending())
                            continue;
                        var client = server.AcceptTcpClient();
                        Task.Run(() =>
                        {
                            Trace.WriteLine("TcpServer.New client: " + client.Client.RemoteEndPoint);
                            if (GotNewClient != null)
                            {
                                client.ReceiveTimeout = ReceiveTimeout;
                                client.SendTimeout = SendTimeout;
                                GotNewClient.Invoke(this, client);
                            }
                            else
                            {
                                client.Close();
                                client.Dispose();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("TcpServer.Listen(). " + ex);
                        ErrorLog?.Invoke(this, ex.ToString());
                    }
                }
                server.Stop();
            });

            _thread.Name = ThreadName;
            _thread.Start();

        }

        public void Stop()
        {
            lock (_runThreadLock)
            {
                _runThread = false;
            }

            _thread.Join(); // wait for the thread to finish
        }

    }
}
