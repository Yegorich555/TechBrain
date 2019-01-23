using System;
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

        Config _config;
        TcpServer _tcpServer;

        public DevServer(Config config, IEnumerable<Device> devices) : this(config)
        {
            DeviceRepository = new DeviceRepository(config.PathDevices, devices);
            DeviceRepository.Commit();
        }

        public DevServer(Config config)
        {
            _config = config;
            DeviceRepository = new DeviceRepository(config.PathDevices);
        }

        #region Public Methods
        public void Start()
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
        }

        public void Stop()
        {
            if (_tcpServer != null)
            {
                _tcpServer.Stop();
                _tcpServer.GotNewClient -= GotNewClient;
            }
            DateTimeService.Instance.HourChanged -= OnHourChanged;
        }
        #endregion

        #region PrivateMethods
        void OnHourChanged(object sender, DateTime now)
        {
            Debug.WriteLine("DevServer. Hour changed => set time");
            var lst = DeviceRepository.GetAll().Where(a => a.HasTime).ToList();
            foreach (var item in lst)
            {
                if (item.WakeUpTime > now || item.IsNeedIp)
                    item.IsWaitSyncTime = true;
                else
                    Task.Run(() => item.SetTime(now));
            }
        }

        void WrapError(Action action, Action finallyAction = null)
        {
            try { action(); }
            catch (Exception ex)
            {
                Debug.WriteLine($"DevServer.ESP. Exception: " + ex);
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
                using (var stream = client.GetStream())
                {
                    Debug.WriteLine($"DevServer.ESP. New client: {client.Client.RemoteEndPoint}");
                    var str = client.WaitResponse("I am");
                    Debug.WriteLine($"DevServer.ESP. Parcel from {client.Client.RemoteEndPoint}: '{str}'");

                    var num = int.Parse(str.Extract('(', ')'));
                    var IpAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    AddOrUpdate(IpAddress, num);

                    byte[] back = Encoding.ASCII.GetBytes("OK\n");
                    stream.Write(back, 0, back.Length);
                }
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
                if (item.HasTime && item.IsWaitSyncTime)
                {
                    Task.Run(() =>
                    {
                        WrapError(() =>
                        {
                            Debug.WriteLine($"DevServer. SyncTime by waiting '{SerialNumber}'");
                            Thread.Sleep(Math.Min(item.ResponseTimeout, 2000));
                            var now = DateTime.Now;
                            if (!(item.WakeUpTime > now))
                                item.SetTime(now);
                        });
                    });
                }
            }
            else
            {
                DeviceRepository.Add(new Device()
                {
                    Type = DeviceTypes.ESP,
                    HasResponse = true,
                    HasSleep = true,
                    IpAddress = IpAddress,
                    SerialNumber = SerialNumber,
                    IsOnline = true,
                });
                DeviceRepository.Commit();
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
