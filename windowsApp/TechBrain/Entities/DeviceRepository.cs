using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                lst = JsonConvert.DeserializeObject<List<Device>>(path);
            }
        }
        public DeviceRepository(string path, IEnumerable<Device> devices)
        {
            _path = path;
            lst = new List<Device>(devices);
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
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
            var json = JsonConvert.SerializeObject(lst, settings);
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
}
