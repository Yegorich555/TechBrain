﻿using Newtonsoft.Json;
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
    public class Device : IEntity
    {
        #region Properties
        public int Id { get; set; }
        public int SerialNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        bool? isOnline;
        [SaveIgnore]
        public bool? IsOnline
        {
            get
            {
                return HasResponse ? isOnline : null;
            }
            set
            {
                isOnline = value;
                if (isOnline == true)
                    IsOnlineDate = DateTime.Now.TruncateToSeconds();
            }

        }

        public DateTime? IsOnlineDate { get; private set; }
        public List<Sensor> Sensors { get; set; }
        public List<Output> Outputs { get; set; }
        public virtual bool HasTime { get; set; }
        public virtual bool HasSleep { get; set; } = true;
        public virtual bool HasResponse { get; set; } = true;

        public int ResponseTimeout { get; set; }
        //public int Repeats { get; set; }
        public DateTime? WakeUpTime { get; set; }

        [SaveIgnore]
        [JsonIgnore]
        public bool IsWaitSyncTime { get; set; }

        #region ESP
        public int? IpPort { get; set; }

        [SaveIgnore]
        public IPAddress IpAddress { get; set; }

        [SaveIgnore]
        [JsonIgnore]
        public bool IsESP { get => Type == DeviceTypes.ESP || Type == DeviceTypes.ESP_AVR; }

        [SaveIgnore]
        [JsonIgnore]
        public bool IsNeedIp { get => IsESP && IpAddress == null; }
        #endregion

        [DefaultValue(OutputTypes.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceTypes Type { get; set; }

        [JsonIgnore]
        public IDriver Driver
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
        public Protocol Protocol
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
        void BaseCommand(Action action)
        {
            if (IsNeedIp)
                throw new DeviceException($"Device does not have IpAddress");
            if (WakeUpTime > DateTime.Now)
                throw new DeviceException($"Device will wake up at {WakeUpTime.Value.ToString("dd HH:mm:ss")}");
            action();
            if (HasResponse)
                IsOnline = true;
        }
        #endregion

        #region PublicMethods
        public bool Ping()
        {
            if (!HasResponse)
                return false;
            IsOnline = Protocol.Ping();
            return IsOnline == true;
        }

        public void SetTime(DateTime dt)
        {
            if (!HasTime)
                throw new DeviceException("Device does not support Time command");
            BaseCommand(() => Protocol.SetTime(dt));
            IsWaitSyncTime = false;
        }

        public void UpdateSensors()
        {
            if (!HasResponse)
                throw new DeviceException("Device does not support Reponse");
            if (Sensors == null || Sensors.Count < 1)
                throw new DeviceException("Device has not Sensors");

            BaseCommand(() => Protocol.UpdateSensors(Sensors));
        }

        public void SetOut(int num, int value) //num == 0 for all outs
        {
            if (Outputs == null || Outputs.Count < 1)
                throw new DeviceException("Device has not Outputs");
            if (Outputs.Count > num || num < 0)
                throw new DeviceException($"Device has not Output {num}");
            if (value > 1 && value < 100) //only 0, 1 or 100 for value digit
            {
                var i = num == 0 ? 0 : num - 1;
                var cnt = num == 0 ? Outputs.Count : i + 1;
                for (; i < cnt; ++i)
                {
                    var output = Outputs[i];
                    if (output.Type != OutputTypes.Pwm)
                        throw new DeviceException($"Device Output {num} is {output.Type.ToString()} and it is not PWM. Output can not take value {value}");
                }
            }

            BaseCommand(() => Protocol.SetOut(num, value));
            if (num == 0)
                Outputs.ForEach(a => a.Value = value);
            else
                Outputs[num - 1].Value = value; //todo not always is actually
        }


        public void Sleep(TimeSpan time)
        {
            if (!HasSleep)
                throw new DeviceException($"Device does not support Sleep");

            if (IsWaitSyncTime && HasTime)
            {
                Debug.WriteLine("Device. Sleep(). Sync time before sleep...");
                BaseCommand(() => Protocol.SetTime(DateTime.Now));
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

            IpAddress = null; //todo storeLatestIpAddress
        }
        #endregion
    }
}
