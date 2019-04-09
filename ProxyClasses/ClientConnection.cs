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
        readonly Cacher cacher;
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
            using (NetworkStream clientStream = client.GetStream())
            {
                byte[] requestBytes = await streamReader.GetBytesFromReading(settings.BufferSize, clientStream);
                string requestInfo = Encoding.ASCII.GetString(requestBytes, 0, requestBytes.Length);
                HttpRequest clientRequest = new HttpRequest(HttpRequest.REQUEST) { LogItemInfo = requestInfo };
                logger.Log(clientRequest);
                if (settings.AllowChangeHeaders) clientRequest.RemoveHeader("User-Agent");

                //check cache for item
                if (cacher.RequestKnown(clientRequest.Method))
                {
                    var knownResponse = cacher.GetKnownResponse(clientRequest.Method);
                    if (!cacher.OlderThanTimeout(knownResponse))
                    {
                        await HandleCachedResponse(logger, settings.BufferSize, clientStream, clientRequest, knownResponse);
                        return;
                    }
                    cacher.RemoveItem(clientRequest.Method);
                }
                // get response from proxy request and send to client
                var responseBytes = await streamReader.MakeProxyRequestAsync(clientRequest, settings.BufferSize);
                cacher.addRequest(clientRequest.Method, responseBytes);
                if (settings.ContentFilterOn) responseBytes = await streamReader.ReplaceImages(responseBytes);
                await streamReader.WriteMessageWithBufferAsync(clientStream, responseBytes, settings.BufferSize);

                string responseString = Encoding.ASCII.GetString(responseBytes, 0, responseBytes.Length);
                HttpRequest proxyResponse = new HttpRequest(HttpRequest.RESPONSE) { LogItemInfo = responseString };
                logger.Log(proxyResponse);
            }
        }
        private async Task HandleCachedResponse(Logger logger, int bufferSize, NetworkStream clientStream, HttpRequest clientRequest, CacheItem knownResponse)
        {
            byte[] knownResponseBytes = knownResponse.ResponseBytes;
            if (settings.ContentFilterOn) knownResponseBytes = await streamReader.ReplaceImages(knownResponseBytes);
            HttpRequest cachedResponse = new HttpRequest(HttpRequest.CACHED_RESPONSE) { LogItemInfo = ASCIIEncoding.ASCII.GetString(knownResponseBytes) };
            if (settings.AllowChangeHeaders) cachedResponse.RemoveHeader("Server");

            knownResponseBytes = await CheckForModifiedContent(logger, bufferSize, clientRequest, knownResponseBytes, knownResponseBytes, cachedResponse);
            if (settings.ContentFilterOn) knownResponseBytes = await streamReader.ReplaceImages(knownResponseBytes);
            await streamReader.WriteMessageWithBufferAsync(clientStream, knownResponseBytes, bufferSize);
            logger.Log(cachedResponse);
        }

        private async Task<byte[]> CheckForModifiedContent(Logger logger, int bufferSize, HttpRequest clientRequest, byte[] responseToSend, byte[] knownResponseBytes, HttpRequest cachedResponse)
        {
            string modifiedDate = cachedResponse.GetHeader("Last-Modified");
            if (modifiedDate != "" && settings.CheckModifiedContent)
            {
                clientRequest.UpdateHeader("If-Modified-Since", $" {modifiedDate}");
                var tmpBytes = await streamReader.MakeProxyRequestAsync(clientRequest, bufferSize);
                if (Encoding.ASCII.GetString(tmpBytes).Contains("304 Not Modified"))
                {
                    logger.Log(new HttpRequest() { LogItemInfo = "Content not modified" });
                    return responseToSend;
                }
                responseToSend = tmpBytes;
                cacher.RemoveItem(clientRequest.Method);
                return responseToSend;
            }
            else responseToSend = knownResponseBytes;
            return responseToSend;
        }
    }
}