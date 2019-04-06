using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyClasses
{
    public class CacheItem
    {
        public DateTime TimeSaved { get; private set; }
        public byte[] ResponseBytes { get; private set; }

        public CacheItem(byte[] requestBytes)
        {
            this.ResponseBytes = requestBytes;
            this.TimeSaved = DateTime.Now;
        }

    }
}
