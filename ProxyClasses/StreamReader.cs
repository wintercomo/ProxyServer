using System;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProxyClasses
{
    public class StreamReader
    {
        readonly byte[] placeholderBytes;
        public StreamReader()
        {
            placeholderBytes = File.ReadAllBytes(@"Assets\Placeholder.png");
        }
        private int BinaryMatch(byte[] input, byte[] pattern)
        {
            int sLen = input.Length - pattern.Length + 1;
            for (int i = 0; i < sLen; ++i)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (input[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
        public async Task<byte[]> MakeProxyRequestAsync(HttpRequest httpRequest, int bufferSize)
        {
            string httpRequestString = httpRequest.HttpString;
            string hostString = httpRequest.GetHeader("Host");
            Uri baseUri = new Uri($"http://{hostString}");
            using (TcpClient proxyTcpClient = new TcpClient())
            {
                await proxyTcpClient.ConnectAsync(baseUri.Host, baseUri.Port);
                using (NetworkStream proxyStream = proxyTcpClient.GetStream())
                {

                    byte[] requestInBytes = Encoding.ASCII.GetBytes(httpRequestString);
                    await WriteMessageWithBufferAsync(proxyStream, requestInBytes, bufferSize);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] responseBytes = await GetBytesFromReading(bufferSize, proxyStream);
                        return responseBytes;
                        //await proxyStream.CopyToAsync(ms);
                        //return ms.ToArray(); ;
                    }
                }
            }
        }
        public async Task WriteMessageWithBufferAsync(NetworkStream destinationStream, byte[] messageBytes, int buffer)
        {
            int index = 0;
            while (index <= messageBytes.Length)
            {
                int remainingBytes = messageBytes.Length - index;
                if (remainingBytes < buffer) await destinationStream.WriteAsync(messageBytes, index, remainingBytes);
                else await destinationStream.WriteAsync(messageBytes, index, buffer);
                index += buffer;
            }
        }
        public async Task<byte[]> ReplaceImages(byte[] message)
        {

            using (MemoryStream memory = new MemoryStream())
            {
                await memory.WriteAsync(message, 0, message.Length);
                memory.Position = 0;
                var index = BinaryMatch(message, Encoding.ASCII.GetBytes("\r\n\r\n")) + 4;
                var headers = Encoding.ASCII.GetString(message, 0, index);
                memory.Position = index;
                if (headers.Contains("Content-Type: image")) await memory.WriteAsync(placeholderBytes, 0, placeholderBytes.Length);
                return memory.ToArray();
            }
        }

        public async Task<byte[]> GetBytesFromReading(int bufferSize, NetworkStream stream)
        {
            byte[] buffer = new byte[bufferSize];
            //use memory stream to save all bytes
            using (MemoryStream memory = new MemoryStream())
            {
                do
                {
                    int readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    await memory.WriteAsync(buffer, 0, readBytes);
                } while (stream.DataAvailable);
                return memory.ToArray();
            }
        }
    }
}
