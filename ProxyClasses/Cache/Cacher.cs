using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyClasses
{
    public class Cacher
    {
        Dictionary<string, CacheItem> knowRequests = new Dictionary<string, CacheItem>();
        public void addRequest(string request, byte[] response)
        {
            CacheItem cacheItem = new CacheItem(response);
            knowRequests.Add(request, cacheItem);
        }
        public void RemoveItem(string key)
        {
            knowRequests.Remove(key);
        }
        public bool OlderThanTimeout(CacheItem item, Int32 cacheTimeout)
        {
            return ((DateTime.Now - item.TimeSaved).TotalSeconds > cacheTimeout);
        }
        public CacheItem GetKnownResponse(string request)
        {
            return knowRequests[request];
        }
        public bool RequestKnown(string request)
        {
            return knowRequests.ContainsKey(request);
        }
    }
}
