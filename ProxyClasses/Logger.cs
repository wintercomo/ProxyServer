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
                    || (!settings.LogContentOut && logItem.Type.Equals(HttpRequest.RESPONSE))) return; // log nothing in this situation
                else
                {
                    if (!settings.LogCLientInfo)
                    {
                        HttpRequest logItemWithoutHeaders = new HttpRequest(logItem.Type) { LogItemInfo = logItem.LogItemInfo };
                        logItemWithoutHeaders.UpdateHeader("User-Agent", "");
                        logItem = logItemWithoutHeaders;
                    }
                    if (!settings.LogRequestHeaders)
                    {
                        HttpRequest logItemWithoutHeaders = new HttpRequest(logItem.Type) { LogItemInfo = logItem.LogItemInfo };
                        logItemWithoutHeaders.ClearHeaders();
                        logItem = logItemWithoutHeaders;
                    }
                    logItems.Add(logItem);
                }
            }
        }
    }
}
