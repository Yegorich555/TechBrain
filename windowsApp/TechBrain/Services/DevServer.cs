using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TechBrain.Entities;
using TechBrain.Extensions;

namespace TechBrain.Services
{
    public class DevServer : IDisposable
    {
        #region Properties
        public event EventHandler<string> ErrorLog;
        public DeviceRepository DeviceRepository { get; set; }
        #endregion

        DevServerConfig _config;
        TcpServer _tcpServer;
        AsyncTimer _scanTimer;

        public DevServer(DevServerConfig config, IEnumerable<Device> devices) : this(config)
        {
            DeviceRepository = new DeviceRepository(config.PathDevices, devices);
            DeviceRepository.Commit();
        }

        public DevServer(DevServerConfig config)
        {
            _config = config;
            DeviceRepository = new DeviceRepository(config.PathDevices);
        }

        #region Public Methods
        public void Start()
        {
            if (paused)
            {
                paused = false;
            }
            else
            {
                _tcpServer = new TcpServer
                {
                    Port = _config.DevServer_TcpPort,
                    ReceiveTimeout = _config.TcpResponseTimeout / 2,
                    SendTimeout = _config.TcpResponseTimeout / 2,
                    ThreadName = "DevServer_ESP_TCP",
                };
                _tcpServer.GotNewClient += GotNewClient;
                _tcpServer.Start();

                DateTimeService.Instance.HourChanged += OnHourChanged;

                _scanTimer = new AsyncTimer(_config.DeviceScanTime);
                _scanTimer.CallBack += Scan_CallBack;
                _scanTimer.Start();
            }
        }

        public void Stop()
        {
            paused = false;
            if (_tcpServer != null)
            {
                _tcpServer.Stop();
                _tcpServer = null;
            }
            DateTimeService.Instance.HourChanged -= OnHourChanged;
            if (_scanTimer != null)
            {
                _scanTimer.Stop();
                _scanTimer.Dispose();
                _scanTimer = null;
            }
        }

        volatile bool paused = false;
        public void PauseScan()
        {
            paused = true;
        }
        #endregion

        #region PrivateMethods
        readonly object lockObj = new object();
        void Scan_CallBack(object sender, CustomEventArgs.CommonEventArgs e)
        {
            if (paused)
            {
                return;
            }
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(lockObj, ref lockTaken);
                if (lockTaken)
                {
                    //Debug.WriteLine("DevServer. Go scan...");
                    var lst = DeviceRepository.GetAll();
                    foreach (var item in lst)
                    {
                        if (item.WakeUpTime > DateTime.Now || item.IsNeedIp) //wait for the future
                            continue;

                        var queue = new List<Action>();
                        if (item.HasTime && item.IsWaitSyncTime)
                            queue.Add(() => item.SetTime(DateTime.Now));

                        if (item.Sensors?.Count > 0)
                        {
                            var now = DateTime.Now;
                            if (item.SensorsNextTime >= now)
                            {
                                queue.Add(() => { item.UpdateSensors(); });
                                item.SensorsNextTime = now.Add(TimeSpan.FromMilliseconds(_config.SensorsScanTime));
                            }
                        }

                        if (item.SleepTime != null)
                            queue.Add(() => item.Sleep());

                        if (item.HasResponse && !queue.Any())
                            queue.Add(() => item.Ping());

                        if (queue.Any())
                        {
                            Task.Run(() => WrapError(() =>
                            {
                                try
                                {
                                    foreach (var action in queue)
                                        action();
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine(ex); //todo Logger
                                }
                            }));
                        }
                    }
                }
                else
                    Trace.WriteLine("DevServer. Skipped scan");
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(lockObj);
            }
        }

        void OnHourChanged(object sender, DateTime now)
        {
            Trace.WriteLine("DevServer. Hour changed => set time");
            var lst = DeviceRepository.GetAll().Where(a => a.HasTime).ToList();
            foreach (var item in lst)
                item.IsWaitSyncTime = true;
        }

        void WrapError(Action action, Action finallyAction = null)
        {
            try { action(); }
            catch (Exception ex)
            {
                Trace.WriteLine($"DevServer.ESP. Exception: " + ex);
                ErrorLog?.Invoke(ex, $"DevServer.ESP. Exception: " + ex);
            }
            finally
            {
                finallyAction?.Invoke();
            }
        }

        void GotNewClient(object sender, TcpClient client)
        {
            WrapError(() =>
            {
                int num;
                IPAddress ip;
                using (var stream = client.GetStream())
                {
                    Trace.WriteLine($"DevServer.ESP. New client: {client.Client.RemoteEndPoint}");
                    var str = client.WaitResponse("I am");
                    Trace.WriteLine($"DevServer.ESP. Parcel from {client.Client.RemoteEndPoint}: '{str}'");

                    num = int.Parse(str.Extract('(', ')'));
                    ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

                    byte[] back = Encoding.ASCII.GetBytes("OK\n");
                    stream.Write(back, 0, back.Length);

                }
                AddOrUpdate(ip, num);
            },
            () =>
            {
                client?.Close();
                client?.Dispose();
            });
        }

        void AddOrUpdate(IPAddress IpAddress, int SerialNumber)
        {
            var item = DeviceRepository.Get(v => v.SerialNumber == SerialNumber);

#if RELEASE
            //find and reset IpAddress for other devices
            var items = DeviceRepository.GetAll().Where(a => IpAddress.Equals(a.IpAddress) && a.Id != item?.Id);
            foreach (var d in items)
                d.IpAddress = null;
#endif
            if (item != null)
            {
                item.IpAddress = IpAddress;
                item.IsOnline = true;
                item.WakeUpTime = null;
            }
            else
            {
                DeviceRepository.Add(new Device(_config.DeviceCacheTime)
                {
                    Type = DeviceTypes.ESP,
                    HasResponse = true,
                    SleepTime = TimeSpan.FromMinutes(1),
                    IpAddress = IpAddress,
                    SerialNumber = SerialNumber,
                    IsOnline = true,
                });
            }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                DeviceRepository = null;
                Stop();
                disposedValue = true;
                _config = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
