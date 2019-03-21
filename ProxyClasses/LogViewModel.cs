using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ProxyClasses
{
    public class LogViewModel:BindableBase
    {
        ObservableCollection<HttpRequest> logItems = new ObservableCollection<HttpRequest>();

        public ObservableCollection<HttpRequest> LogItems { get => logItems;
            set
            {
                if (SetProperty<ObservableCollection<HttpRequest>>(ref logItems, value))
                {
                    logItems = value;
                }
            }
        }
        public void Add(HttpRequest item)
        {
            logItems.Add(item);
        }
    }
}
