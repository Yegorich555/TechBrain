using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using TechBrain.Extensions;
using TechBrain.IO;

namespace TechBrain.Entities
{
    public class DeviceRepository
    {
        string _path;
        object _lockObj = new object();
        List<Device> lst;

        void InitDevices(string path, List<Device> devices)
        {
            _path = path;
            lst = devices;

            foreach (var item in lst) //check for invalid id
                item.Id = GenerateId(item); //create new id if it was invalid
        }

        public DeviceRepository(string path)
        {
            List<Device> devices;
            if (FileSystem.TryRead(path, out string text))
                devices = JsonConvert.DeserializeObject<List<Device>>(text, SerializerSettings);
            else
                devices = new List<Device>();
            InitDevices(path, devices);
        }
        public DeviceRepository(string path, IEnumerable<Device> devices) => InitDevices(path, new List<Device>());

        public int Count { get => lst.Count; }

        public static JsonSerializerSettings SerializerSettings
        {
            get
            {
                var settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    ContractResolver = new RepositoryContractResolver()
                };
                settings.Converters.Add(new IPAddressConverter());
                return settings;
            }
        }

        public Device this[int index]
        {
            get
            {
                lock (_lockObj)
                    return lst[index];
            }
            set
            {
                lock (_lockObj)
                    lst[index] = value;
            }
        }

        T ChangeAction<T>(Func<T> action)
        {
            lock (_lockObj)
            {
                var v = action();
                BaseCommit();
                return v;
            }
        }

        void BaseCommit()
        {
            var json = JsonConvert.SerializeObject(lst, SerializerSettings);
            File.WriteAllText(_path, json);
        }

        public void Commit()
        {
            lock (_lockObj)
                BaseCommit();
        }

        int GenerateId(Device device)
        {
            var id = device.Id;
            if (id <= 0)
                id = lst[lst.Count - 1].Id + 1;
            while (lst.Any(a => a.Id == id && device != a))
                ++id;
            return id;
        }

        public int Add(Device device)
        {
            return ChangeAction(() =>
            {
                device.Id = GenerateId(device);
                lst.Add(device);
                return device.Id;
            });
        }

        public void Remove(int id)
        {
            ChangeAction(() =>
            {
                return lst.Remove(lst.First(a => a.Id == id));
            });
        }


        public List<Device> GetAll()
        {
            lock (_lockObj)
                return lst.ToList();
        }

        public Device Get(int id)
        {
            lock (_lockObj)
                return lst.First(a => a.Id == id);
        }

        public Device Get(Func<Device, bool> predicate)
        {
            lock (_lockObj)
                return lst.FirstOrDefault(predicate);
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class SaveIgnoreAttribute : Attribute
    { }

    public class RepositoryContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.Ignored = property.Ignored || member.GetCustomAttribute<SaveIgnoreAttribute>() != null;
            if (!property.Ignored)
            {
                var attr = member.GetCustomAttribute<DefaultValueAttribute>();
                //if (attr != null)
                //{
                //    //var val = attr.Value;
                //    var val = property.GetResolvedDefaultValue();
                //    member.

                //}
            }
            return property;
        }
    }
}
