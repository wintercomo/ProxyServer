using ProxyServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

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
        private readonly string type;
        private string method;
        private string body;
        public HttpRequest(string type = HttpRequest.MESSAGE)
        {
            this.type = type;
        }
        public Dictionary<string, string> getHeadersList { get; } = new Dictionary<string, string>();
        public string Method
        {
            get { return this.method; }
            private set
            {
                if (SetProperty<string>(ref method, value)) this.method = value;
            }
        }
        public string Type
        {
            get { return this.type; }
        }

        internal void ClearHeaders()
        {
            getHeadersList.Clear();
        }

        public string Headers
        {
            get
            {
                string sm = "";
                foreach (KeyValuePair<string, string> entry in getHeadersList) sm += $"{entry.Key}:{entry.Value}\r\n";
                return sm;
            }
        }

        internal void RemoveHeader(string v)
        {
            getHeadersList.Remove(v);
        }

        public string Body
        {
            get { return this.body; }
        }
        public string HttpString
        {
            get
            {
                // set headers to disable browser cache
                UpdateHeader("Connection", " Close");
                return $"{Method}\r\n{Headers}\r\n{Body}";
            }
        }
        //TODO edit this function so the type is used to display different data
        public string LogItemInfo
        {
            get => this.type.Equals(HttpRequest.MESSAGE) ? logItemInfo : $"{Method}\r\n{Headers}\r\n{Body}";
            set
            {
                SeperateProtocolElements(value);
                if (SetProperty<string>(ref logItemInfo, value)) this.logItemInfo = value;
            }
        }


        // Get the method/headers and body and save them seperately for later use
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
                
                else this.body += result[i];
            }
        }
        public void UpdateHeader(string headerType, string header)
        {
            if (getHeadersList.ContainsKey(headerType)) getHeadersList.Remove(headerType);
            getHeadersList.Add(headerType, header);
        }
        public string GetHeader(string headerType)
        {
            if (getHeadersList.ContainsKey(headerType)) return getHeadersList[headerType];
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
    }
}
