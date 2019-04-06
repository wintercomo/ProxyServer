using ProxyServer;
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
        Cacher cacher;
        public TcpConnection(TcpClient client, ProxySettingsViewModel settings, Cacher cacher)
        {
            this.client = client;
            streamReader = new StreamReader();
            this.settings = settings;
            this.cacher = cacher;
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

            if (cacher.RequestKnown(clientRequest.Method))
            {
                var knownResponse = cacher.GetKnownResponse(clientRequest.Method);
                var knownResponseBytes = knownResponse.ResponseBytes;
                if (settings.ContentFilterOn) knownResponseBytes = await streamReader.ReplaceImages(knownResponseBytes);
                await streamReader.WriteMessageWithBufferAsync(clientStream, knownResponseBytes, bufferSize);
                logger.Log(new HttpRequest(HttpRequest.CACHED_RESPONSE) { LogItemInfo = ASCIIEncoding.ASCII.GetString(knownResponseBytes) });
                return;
            }

            // get response from proxy request
            var responseBytes = await streamReader.MakeProxyRequestAsync(clientRequest, bufferSize);

            if (settings.ContentFilterOn) responseBytes = await streamReader.ReplaceImages(responseBytes);
            cacher.addRequest(clientRequest.Method, responseBytes);
            await streamReader.WriteMessageWithBufferAsync(clientStream, responseBytes, bufferSize);


            string responseString = Encoding.ASCII.GetString(responseBytes, 0, responseBytes.Length);
            HttpRequest proxyResponse = new HttpRequest(HttpRequest.RESPONSE) { LogItemInfo = responseString };
            logger.Log(proxyResponse);
        }

        internal void CloseConnection()
        {
            client.Dispose();
        }
    }
}