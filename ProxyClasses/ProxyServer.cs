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
        TcpListener serverListener;
        ProxySettingsViewModel settings;

        public ProxyServer(ProxySettingsViewModel proxySettings)
        {
            serverListener = new TcpListener(IPAddress.Any, proxySettings.Port);
            this.settings = proxySettings;
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
            TcpClient newClient = await serverListener.AcceptTcpClientAsync();
            TcpConnection client = new TcpConnection(newClient, settings);
            await client.HandleHttpRequestsAsync(logger);
            client.CloseConnection();
        }
    }
}
