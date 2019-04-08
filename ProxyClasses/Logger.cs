using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using ProxyServer;

namespace ProxyClasses
{
    public class Logger
    {
        readonly ObservableCollection<HttpRequest> logItems;
        readonly ProxySettingsViewModel settings;
        object _itemsLock;
        public Logger(ObservableCollection<HttpRequest> logItems, ProxySettingsViewModel settings, object _itemsLock)
        {
            this.logItems = logItems;
            this.settings = settings;
            this._itemsLock = _itemsLock;
        }

        public object BindingOperations { get; }

        public void Log(HttpRequest logItem)
        {
            lock (_itemsLock)
            {
                if ((!settings.LogContentIn && logItem.Type.Equals(HttpRequest.REQUEST))
                    || (!settings.LogContentOut && logItem.Type.Equals(HttpRequest.RESPONSE))) return; // do nothing in this situation
                else
                {
                    if (!settings.LogCLientInfo) logItem.UpdateHeader("User-Agent", "");
                    if (!settings.LogRequestHeaders)
                    {
                        //Make a copy of item so request wont fail
                        HttpRequest logItemWithoutHeaders = new HttpRequest(logItem.Type) { LogItemInfo = logItem.LogItemInfo };
                        logItemWithoutHeaders.ClearHeaders();
                        logItems.Add(logItemWithoutHeaders);
                        return;
                    }
                    logItems.Add(logItem);
                }
            }
        }
    }
}
