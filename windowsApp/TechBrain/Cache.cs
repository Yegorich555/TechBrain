using System;
using System.Collections.Concurrent;

namespace TechBrain
{
    public struct CacheValue<TValue>
    {
        public CacheValue(TValue value, DateTime expiryDate)
        {
            ExpiryDate = expiryDate;
            Value = value;
        }
        public DateTime ExpiryDate { get; set; }
        public TValue Value { get; set; }
    }

    public class Cache<TKey, TValue>
    {
        ConcurrentDictionary<TKey, CacheValue<TValue>> lst = new ConcurrentDictionary<TKey, CacheValue<TValue>>();

        public bool TryGet(TKey key, out TValue value)
        {
            if (lst.TryGetValue(key, out var cacheValue))
            {
                if (cacheValue.ExpiryDate >= DateTime.Now)
                {
                    value = cacheValue.Value;
                    return true;
                }
                else
                    lst.TryRemove(key, out var v);
            }
            value = default(TValue);
            return false;
        }

        public void Set(TKey key, TValue value, TimeSpan timeSpan)
        {
            var expDate = DateTime.Now.Add(timeSpan);
            lst.AddOrUpdate(
                key,
                (k) => new CacheValue<TValue>(value, expDate),
                (k, vp) =>
                {
                    vp.ExpiryDate = expDate;
                    return vp;
                }
            );
        }
    }
}
