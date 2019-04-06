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
            if (settings.AllowChangeHeaders) clientRequest.RemoveHeader("User-Agent");

            //check cache for item
            if (cacher.RequestKnown(clientRequest.Method))
            {
                var knownResponse = cacher.GetKnownResponse(clientRequest.Method);
                if (!cacher.OlderThanTimeout(knownResponse)) await HandleCachedResponse(logger, bufferSize, clientStream, clientRequest, knownResponse);
            }
            else
            {
                // get response from proxy request and send to client
                var responseBytes = await streamReader.MakeProxyRequestAsync(clientRequest, bufferSize);
                cacher.addRequest(clientRequest.Method, responseBytes);
                if (settings.ContentFilterOn) responseBytes = await streamReader.ReplaceImages(responseBytes);
                await streamReader.WriteMessageWithBufferAsync(clientStream, responseBytes, bufferSize);

                string responseString = Encoding.ASCII.GetString(responseBytes, 0, responseBytes.Length);
                HttpRequest proxyResponse = new HttpRequest(HttpRequest.RESPONSE) { LogItemInfo = responseString };
                if (settings.AllowChangeHeaders) proxyResponse.RemoveHeader("Server");
                logger.Log(proxyResponse);
            }
        }

        private async Task HandleCachedResponse(Logger logger, int bufferSize, NetworkStream clientStream, HttpRequest clientRequest, CacheItem knownResponse)
        {
            byte[] knownResponseBytes = knownResponse.ResponseBytes;
            if (settings.ContentFilterOn) knownResponseBytes = await streamReader.ReplaceImages(knownResponseBytes);
            HttpRequest cachedResponse = new HttpRequest(HttpRequest.CACHED_RESPONSE) { LogItemInfo = ASCIIEncoding.ASCII.GetString(knownResponseBytes) };

            knownResponseBytes = await CheckForModifiedContent(logger, bufferSize, clientRequest, knownResponseBytes, knownResponseBytes, cachedResponse);
            if (settings.AllowChangeHeaders) cachedResponse.RemoveHeader("Server");
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
                if (!ASCIIEncoding.ASCII.GetString(tmpBytes).Contains("304 Not Modified"))
                {
                    responseToSend = tmpBytes;
                    if (settings.AllowChangeHeaders) cachedResponse.RemoveHeader("Server");
                    //await streamReader.WriteMessageWithBufferAsync(clientStream, tmpBytes, bufferSize);
                    logger.Log(new HttpRequest() { LogItemInfo = "Content not modified" });
                    cacher.RemoveItem(clientRequest.Method);
                    //return;
                }
            }
            else
            {
                responseToSend = knownResponseBytes;
            }

            return responseToSend;
        }

        internal void CloseConnection()
        {
            client.Dispose();
        }
    }
}