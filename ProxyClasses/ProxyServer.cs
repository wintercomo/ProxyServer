using ProxyServer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyClasses
{
    public class ProxyServer

    {
        readonly TcpListener serverListener;
        readonly ProxySettingsViewModel settings;
        readonly Cacher cacher;
        public ProxyServer(ProxySettingsViewModel proxySettings)
        {
            this.serverListener = new TcpListener(IPAddress.Any, proxySettings.Port);
            this.settings = proxySettings;
            this.cacher = new Cacher(settings);
        }

        public void StartServer()
        {
            serverListener.Start();
        }

        public void StopServer()
        {
            serverListener.Stop();
        }

        public async Task AcceptTcpClientAsync(Logger logger)
        {
            using (TcpClient newClient = await serverListener.AcceptTcpClientAsync())
            {
                TcpConnection client = new TcpConnection(newClient, settings, cacher);
                await client.HandleHttpRequestsAsync(logger);
            }
        }
    }
}
