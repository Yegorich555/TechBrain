﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TechBrain.Communication.Drivers;
using TechBrain.Communication.Protocols;

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
        public int Id { get; set; }
        public int SerialNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        bool? isOnline;
        public bool? IsOnline
        {
            get { return HasResponse ? isOnline : null; }
            set
            {
                isOnline = value;
                if (isOnline == true)
                    IsOnlineDate = DateTime.Now;
            }

        }

        public DateTime? IsOnlineDate { get; private set; }
        public IList<Sensor> Sensors { get; set; }
        public IList<DeviceOutput> Outputs { get; set; }
        public virtual bool HasTime { get; set; }
        public virtual bool HasSleep { get; set; } = true;
        public virtual bool HasResponse { get; set; } = true;

        public int ResponseTimeout { get; set; }
        //public int Repeats { get; set; }

        #region ESP
        public int? IpPort { get; set; }
        public IPAddress IpAddress { get; set; }
        #endregion

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
                        return new TbProtocol(Driver, HasResponse, Id); //todo use TbProtocol.Address instead of SerialNumber
                    case DeviceTypes.ESP_AVR:
                        return new TbProtocol(Driver, HasResponse, TbProtocol.BroadcastAddr);
                    case DeviceTypes.ESP:
                        return new EspProtocol(Driver);
                }
                throw new NotImplementedException();
            }
        }

        public bool Ping()
        {
            if (!HasResponse)
                return false;
            IsOnline = Protocol.Ping();
            return IsOnline == true;
        }

        public bool SetTime(DateTime dt)
        {
            if (!HasResponse && !HasTime)
                return false;
            IsOnline = Protocol.SetTime(dt);
            return true;
        }

        public bool UpdateSensors()
        {
            if (!HasResponse || Sensors == null || Sensors.Count < 1) //todo maybe create sensors list
                return false;
            IsOnline = Protocol.UpdateSensors(Sensors);
            return true;
        }

        public bool SetOut(int num, int value)
        {
            if (!HasResponse || Outputs == null || Outputs.Count < 1)
                return false;
            if (Outputs.Count > num || num < 0)
                return false;
            if (value > 1 && value < 100) //for value can be 1 for digit
            {
                if (num == 0 && Outputs.Any(a => a.Type != OutputTypes.Pwm)) //that's for all outputs
                    return false;
                else if (Outputs[num - 1].Type != OutputTypes.Pwm)
                    return false;
            }

            // IsOnline = Protocol
            Protocol.SetOut(num, value);
            IsOnline = true;

            return true;
        }

        public bool Sleep(TimeSpan time)
        {
            if (!HasSleep)
                return false;

            if (Type == DeviceTypes.ESP || Type == DeviceTypes.ESP_AVR)
            {
                var driver = new TcpDriver(IpAddress, (int)IpPort)
                {
                    ResponseTimeout = ResponseTimeout,
                };
                var protocol = new EspProtocol(driver);
                protocol.Sleep(time);
                //todo store end sleepTimeSpan
                return true;
            }

            return false;
        }
    }
}
