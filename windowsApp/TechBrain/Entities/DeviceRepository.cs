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

        public DeviceRepository(string path)
        {
            _path = path;
            if (FileSystem.ExistPath(path))
            {
                var text = File.ReadAllText(path);
                lst = JsonConvert.DeserializeObject<List<Device>>(text, SerializerSettings);
            }
            else
                lst = new List<Device>();
        }
        public DeviceRepository(string path, IEnumerable<Device> devices)
        {
            _path = path;
            lst = new List<Device>(devices);
        }

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

        public int Add(Device device)
        {
            return ChangeAction(() =>
            {
                var id = device.Id;
                if (id == 0 || lst.Any(a => a.Id == id))
                    id = lst[lst.Count - 1].Id + 1;
                while (lst.Any(a => a.Id == id))
                    ++id;

                device.Id = id;
                lst.Add(device);
                return id;
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
