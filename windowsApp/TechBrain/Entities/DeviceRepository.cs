using System;
using System.Collections.Generic;
using System.Linq;

namespace TechBrain.Entities
{
    public class DeviceRepository
    {
        object _lockObj = new object();
        List<Device> lst;
        public DeviceRepository() => lst = new List<Device>();
        public DeviceRepository(IEnumerable<Device> devices) => lst = new List<Device>(devices);

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

        public int Add(Device device)
        {
            lock (_lockObj)
            {
                var id = device.Id;
                if (id == 0 || lst.Any(a => a.Id == id))
                    id = lst[lst.Count - 1].Id + 1;
                while (lst.Any(a => a.Id == id))
                    ++id;

                device.Id = id;
                lst.Add(device);
                return id;
            }
        }

        public void Remove(int id)
        {
            lock (_lockObj)
                lst.Remove(lst.First(a => a.Id == id));
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
