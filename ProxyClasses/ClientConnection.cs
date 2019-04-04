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
        TcpClient client;
        StreamReader streamReader;
        ProxySettingsViewModel settings;
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
            Console.WriteLine($"BUFFER : {settings.BufferSize}");
            var clientStream = client.GetStream();
            var requestBytes = await streamReader.GetBytesFromReading(bufferSize, clientStream);
            string requestInfo = ASCIIEncoding.ASCII.GetString(requestBytes, 0, requestBytes.Length);
            HttpRequest clientRequest = new HttpRequest(HttpRequest.REQUEST) { LogItemInfo = requestInfo };
            logger.Log(clientRequest);

            // get response from proxy request
            string hostString = clientRequest.GetHeader("Host");
            Uri baseUri = new Uri($"http://{hostString}");
            TcpClient proxyTcpClient = new TcpClient();
            await proxyTcpClient.ConnectAsync(baseUri.Host, baseUri.Port);

            var responseData = await streamReader.MakeProxyRequestAsync(clientRequest, bufferSize);
            //_ = Task.Run(async () => await streamReader.WriteMessageWithBufferAsync(clientStream, responseData, bufferSize));
            await streamReader.WriteMessageWithBufferAsync(clientStream, responseData, bufferSize);
            //await clientStream.WriteAsync(responseData, 0, responseData.Length);
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
//}
//using (NetworkStream proxyStream = proxyTcpClient.GetStream())
//            {

//                await streamReader.WriteMessageWithBufferAsync(proxyStream, requestBytes, bufferSize);
//                byte[] buffer = new byte[bufferSize];
//                {
//                    do
//                    {
//                        int readBytes = await proxyStream.ReadAsync(buffer, 0, buffer.Length);
//                        await clientStream.WriteAsync(buffer, 0, buffer.Length);
//                    } while (proxyStream.DataAvailable);
//                }
//            }
