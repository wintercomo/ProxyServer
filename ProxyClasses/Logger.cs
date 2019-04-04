using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ProxyClasses
{
    public class Logger
    {
        ObservableCollection<HttpRequest> logItems;
        public Logger(ObservableCollection<HttpRequest> logItems)
        {
            this.logItems = logItems;
        }

        public void Log(HttpRequest logItem)
        {
            logItems.Add(logItem);
        }
    }
}
