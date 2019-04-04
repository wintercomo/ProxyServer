﻿using ProxyServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyClasses
{
    class TcpConnection
    {
        readonly TcpClient client;
        readonly StreamReader streamReader;
        readonly ProxySettingsViewModel settings;
        public TcpConnection(TcpClient client, ProxySettingsViewModel settings)
        {
            this.client = client;
            streamReader = new StreamReader();
            this.settings = settings;
        }

        internal async Task HandleHttpRequestsAsync(Logger logger)
        {
            // get request info and make a request as proxy
            int bufferSize = settings.BufferSize;
            var clientStream = client.GetStream();
            var requestBytes = await streamReader.GetBytesFromReading(bufferSize, clientStream);
            string requestInfo = Encoding.ASCII.GetString(requestBytes, 0, requestBytes.Length);
            HttpRequest clientRequest = new HttpRequest(HttpRequest.REQUEST) { LogItemInfo = requestInfo };
            logger.Log(clientRequest);

            // get response from proxy request
            var responseData = await streamReader.MakeProxyRequestAsync(clientRequest, bufferSize);
            await streamReader.WriteMessageWithBufferAsync(clientStream, responseData, bufferSize);
            string responseString = Encoding.ASCII.GetString(responseData, 0, responseData.Length);
            HttpRequest proxyResponse = new HttpRequest(HttpRequest.RESPONSE) { LogItemInfo = responseString };
            logger.Log(proxyResponse);
        }

        internal void CloseConnection()
        {
            client.Dispose();
        }
    }
}