using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyClasses
{
    public class CacheItem
    {
        private byte[] responseBytes;
        DateTime timeSaved;
        public DateTime TimeSaved { get => timeSaved; set => timeSaved = value; }
        public byte[] ResponseBytes { get => responseBytes; set => responseBytes = value; }

        public CacheItem(byte[] requestBytes)
        {
            this.responseBytes = requestBytes;
            this.timeSaved = DateTime.Now;
        }

    }
}
