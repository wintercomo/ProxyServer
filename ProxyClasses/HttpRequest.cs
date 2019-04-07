using ProxyServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProxyClasses
{
    public class HttpRequest : BindableBase
    {
        public const string REQUEST = "REQUEST";
        public const string RESPONSE = "RESPONSE";
        public const string CACHED_RESPONSE = "CACHED RESPONSE";
        public const string MESSAGE = "MESSAGE";
        public const string ERROR = "ERROR";
        private string logItemInfo;
        private string method;

        public HttpRequest(string type = HttpRequest.MESSAGE)
        {
            this.Type = type;
        }

        public byte[] MessageBytes { get; set; }

        private readonly Dictionary<string, string> getHeadersList = new Dictionary<string, string>();

        public Dictionary<string, string> GetGetHeadersList()
        {
            return getHeadersList;
        }

        public string Method
        {
            get => this.method;
            private set
            {
                if (SetProperty<string>(ref method, value)) this.method = value;
            }
        }
        public string Type { get; }


        public string Headers
        {
            get
            {
                string sm = "";
                foreach (KeyValuePair<string, string> entry in GetGetHeadersList()) sm += $"{entry.Key}:{entry.Value}\r\n";
                return sm;
            }
        }

        internal void RemoveHeader(string v)
        {
            GetGetHeadersList().Remove(v);
        }

        public string Body { get; private set; }
        public string HttpString
        {
            get
            {
                // set headers to disable browser cache
                UpdateHeader("Connection", " Close");
                return $"{Method}\r\n{Headers}\r\n{Body}";
            }
        }
        public string LogItemInfo
        {
            get => this.Type.Equals(HttpRequest.MESSAGE) ? logItemInfo : $"{Method}\r\n{Headers}\r\n{Body}";
            set
            {
                SeperateProtocolElements(value);
                if (SetProperty<string>(ref logItemInfo, value)) this.logItemInfo = value;
            }
        }
        // Get the method/headers and body and save them seperately
        private void SeperateProtocolElements(string value)
        {
            bool reachedBody = false;
            string[] result = Regex.Split(value, "\r\n|\r|\n");
            for (int i = 0; i < result.Length; i++)
            {
                if (i == 0) this.Method = result[i];
                else if (i > 0 && !reachedBody)
                {
                    if (result[i] == "") reachedBody = true;
                    else SaveHeader(result, i);
                }

                else this.Body += result[i];
            }
        }

        public void UpdateHeader(string headerType, string header)
        {
            if (GetGetHeadersList().ContainsKey(headerType)) GetGetHeadersList().Remove(headerType);
            GetGetHeadersList().Add(headerType, header);
        }
        public string GetHeader(string headerType)
        {
            if (GetGetHeadersList().ContainsKey(headerType)) return GetGetHeadersList()[headerType];
            return "";
        }
        private void SaveHeader(string[] result, int i)
        {
            int index = result[i].IndexOf(':');
            if (index != -1)
            {
                string headerType = result[i].Substring(0, index);
                string header = result[i].Substring(index + 2); // + 2 to remove the : and space
                UpdateHeader(headerType, header);
            }
        }
        internal void ClearHeaders()
        {
            GetGetHeadersList().Clear();
        }
    }
}
