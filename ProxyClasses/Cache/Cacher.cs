using ProxyServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyClasses
{
    public class Cacher
    {
        Dictionary<string, CacheItem> knowRequests = new Dictionary<string, CacheItem>();
        ProxySettingsViewModel settings;
        public Cacher(ProxySettingsViewModel settings)
        {
            this.settings = settings;
        }
        public void addRequest(string request, byte[] response)
        {
            CacheItem cacheItem = new CacheItem(response);
            knowRequests.Add(request, cacheItem);
        }
        public void RemoveItem(string key)
        {
            knowRequests.Remove(key);
        }
        public bool OlderThanTimeout(CacheItem item)
        {
            return ((DateTime.Now - item.TimeSaved).TotalSeconds > settings.CacheTimeout);
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
