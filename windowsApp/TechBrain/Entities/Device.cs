using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using TechBrain.Communication.Drivers;
using TechBrain.Communication.Protocols;

namespace TechBrain.Entities
{
    public enum DeviceTypes
    {
        None,
        AVR,
        ESP,
        ESP_AVR
    }
    public class Device : IEntity
    {
        public int SerialNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        bool isOnline;
        public bool IsOnline
        {
            get { return isOnline; }
            set
            {
                isOnline = value;
                if (isOnline)
                    IsOnlineDate = DateTime.Now;
            }

        }

        public DateTime IsOnlineDate { get; private set; }
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
                        return new TbProtocol(Driver, SerialNumber);
                    case DeviceTypes.ESP_AVR:
                        return new TbProtocol(Driver);
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
            return IsOnline;
        }

    }
}
