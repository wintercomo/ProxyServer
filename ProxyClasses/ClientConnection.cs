using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyClasses
{
    class TcpConnection
    {
        TcpClient client;
        StreamReader streamReader;
        public TcpConnection(TcpClient client)
        {
            this.client = client;
            streamReader = new StreamReader();
        }

        internal async Task HandleHttpRequestsAsync(int bufferSize, Logger logger)
        {
            var clientStream = client.GetStream();
            byte[] requestBytes = await streamReader.GetBytesFromReading(bufferSize, clientStream);
            string requestInfo = ASCIIEncoding.ASCII.GetString(requestBytes, 0, requestBytes.Length);
            HttpRequest clientRequest = new HttpRequest(HttpRequest.REQUEST) { LogItemInfo = requestInfo };
            logger.Log(clientRequest);


            var responseData = await streamReader.MakeProxyRequestAsync(clientRequest, bufferSize);
            await clientStream.WriteAsync(responseData, 0, responseData.Length);
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
