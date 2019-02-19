using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using TechBrain.Communication.Drivers;
using TechBrain.Communication.Protocols;
using TechBrain.Extensions;

namespace TechBrain.Entities
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeviceTypes
    {
        None,
        AVR,
        ESP,
        ESP_AVR
    }

    enum CacheKeys
    {
        Ping,
        SetTime,
        UpdateSensors
    }

    public class Device : IEntity
    {
        readonly Cache<CacheKeys, bool> _cache = new Cache<CacheKeys, bool>();
        readonly int _cacheTime;

        public Device(int cacheTime)
        {
            _cacheTime = cacheTime;
        }

        private Device() { }

        #region Properties
        public int Id { get; set; }
        public int SerialNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        bool? _isOnline;
        [SaveIgnore]
        public bool? IsOnline
        {
            get => HasResponse ? _isOnline : null;
            set
            {
                _isOnline = value;
                if (_isOnline == true)
                    IsOnlineDate = DateTime.Now.TruncateToSeconds();
            }

        }

        public DateTime? IsOnlineDate { get; private set; }
        public List<Sensor> Sensors { get; set; }
        public List<Output> Outputs { get; set; }
        public virtual bool HasTime { get; set; }

        public virtual bool HasResponse { get; set; } = true;
        public int ResponseTimeout { get; set; } = 5000;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ResponseRepeats { get; set; }

        public TimeSpan? SleepTime { get; set; }

        DateTime? _wakeUpTime;
        public DateTime? WakeUpTime
        {
            get => _wakeUpTime;
            set
            {
                _wakeUpTime = value;
                if (_wakeUpTime.HasValue)
                    _wakeUpTime.Value.TruncateToSeconds().AddSeconds(10);

            }
        }
        public int? IpPort { get; set; }

        IPAddress ipAddress;
        [SaveIgnore]
        public IPAddress IpAddress
        {
            get => ipAddress;
            set
            {
                ipAddress = value;
                _isNeedIp = false;
            }
        }

        #region Helpers
        [SaveIgnore]
        [JsonIgnore]
        public bool IsESP { get => Type == DeviceTypes.ESP || Type == DeviceTypes.ESP_AVR; }

        bool _isNeedIp = false;
        [SaveIgnore]
        [JsonIgnore]
        public bool IsNeedIp { get => IsESP && (_isNeedIp || IpAddress == null); }

        internal bool IsWaitSyncTime { get; set; }
        internal DateTime SensorsNextTime { get; set; } = DateTime.MinValue;
        #endregion

        [DefaultValue(OutputTypes.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceTypes Type { get; set; }

        [JsonIgnore]
        IDriver Driver
        {
            get
            {
                switch (Type)
                {
                    case DeviceTypes.None:
                        throw new NullReferenceException("Device type is not defined");
                    case DeviceTypes.AVR:
                        break;
                    case DeviceTypes.ESP:
                    case DeviceTypes.ESP_AVR:
                        return new TcpDriver(IpAddress, (int)IpPort)
                        {
                            ResponseTimeout = ResponseTimeout,
                        };
                }
                throw new NotImplementedException();
            }
        }

        [JsonIgnore]
        Protocol Protocol
        {
            get
            {
                switch (Type)
                {
                    case DeviceTypes.None:
                        throw new NullReferenceException("Device type is not defined");
                    case DeviceTypes.AVR:
                        return new TbProtocol(Driver, HasResponse, SerialNumber);
                    case DeviceTypes.ESP_AVR:
                        return new TbProtocol(Driver, HasResponse, TbProtocol.BroadcastAddr);
                    case DeviceTypes.ESP:
                        return new EspProtocol(Driver);
                }
                throw new NotImplementedException();
            }
        }

        #endregion

        #region PrivateMethods
        readonly object lockObj = new object();
        void BaseCommand(Action action, CacheKeys? cacheKey = null)
        {
            if (cacheKey != null && _cache.TryGet((CacheKeys)cacheKey, out var v))
            {
                Trace.WriteLine("Device. Result from Cache");
                return;
            }
            lock (lockObj)
            {
                if (IsNeedIp)
                    throw new DeviceException($"Device does not have IpAddress");
                if (WakeUpTime > DateTime.Now)
                    throw new DeviceException($"Device will wake up at {WakeUpTime.Value.ToString("dd HH:mm:ss")}");

                int i = 0;
                while (true)
                {
                    try { action(); }
                    catch (TimeoutException)
                    {
                        if (++i >= ResponseRepeats)
                            throw;
                    }
                    break;
                }

                if (cacheKey != null)
                    _cache.Set((CacheKeys)cacheKey, true, TimeSpan.FromMilliseconds(_cacheTime));

                if (HasResponse)
                    IsOnline = true;
            }
        }

        #endregion

        #region PublicMethods
        public bool Ping()
        {
            if (!HasResponse)
                throw new DeviceException($"Device does not support Ping command"); ;

            BaseCommand(() =>
            {
                var i = 0;
                while (true)
                {
                    var ok = Protocol.Ping();
                    IsOnline = ok;
                    if (!ok || ++i >= ResponseRepeats)
                        break;
                }
            }, CacheKeys.Ping);
            return IsOnline == true;
        }

        public void SetTime(DateTime dt)
        {
            if (!HasTime)
                throw new DeviceException("Device does not support Time command");
            BaseCommand(() => Protocol.SetTime(dt), CacheKeys.SetTime);
            IsWaitSyncTime = false;
        }

        public void UpdateSensors()
        {
            if (!HasResponse)
                throw new DeviceException("Device does not support Reponse");
            if (Sensors == null || Sensors.Count < 1)
                throw new DeviceException("Device has not Sensors");

            BaseCommand(() => Protocol.UpdateSensors(Sensors), CacheKeys.UpdateSensors);
        }

        public void SetOut(int num, int value) //num == 0 for all outs
        {
            if (Outputs == null || Outputs.Count < 1)
                throw new DeviceException("Device has not Outputs");
            if (Outputs.Count > num || num < 0)
                throw new DeviceException($"Device has not Output {num}");

            var isNeedChange = false;
            var isPwmValue = value > 1 && value < 100;//only 0, 1 or 100 for value digit
            var i = num == 0 ? 0 : num - 1;
            var cnt = num == 0 ? Outputs.Count : i + 1;
            for (; i < cnt; ++i)
            {
                var output = Outputs[i];
                if (isPwmValue && output.Type != OutputTypes.Pwm)
                    throw new DeviceException($"Device Output {num} is {output.Type.ToString()} and it is not PWM. Output can not take value {value}");

                if (output.Value != value)
                    isNeedChange = true;
            }

            if (!isNeedChange) //nothing to change
                return;

            BaseCommand(() => Protocol.SetOut(num, value));

            if (num == 0)
                Outputs.ForEach(a => a.Value = value);
            else
                Outputs[num - 1].Value = value;
        }

        public void Sleep()
        {
            if (SleepTime == null)
                throw new DeviceException($"Device doesn't have sleep time");
        }

        public void Sleep(TimeSpan time)
        {
            if (IsWaitSyncTime && HasTime)
            {
                Trace.WriteLine("Device. Sleep(). Sync time before sleep...");
                BaseCommand(() => Protocol.SetTime(DateTime.Now), CacheKeys.SetTime);
            }

            Protocol sleepProtocol;
            if (IsESP)
            {
                var driver = new TcpDriver(IpAddress, (int)IpPort)
                {
                    ResponseTimeout = ResponseTimeout,
                };
                sleepProtocol = new EspProtocol(driver);
            }
            else
                sleepProtocol = Protocol;

            BaseCommand(() => sleepProtocol.Sleep(time));

            if (HasResponse)
                IsOnlineDate = DateTime.Now;
            IsOnline = false;
            WakeUpTime = DateTime.Now.Add(time);

            _isNeedIp = true;
        }
        #endregion
    }
}
