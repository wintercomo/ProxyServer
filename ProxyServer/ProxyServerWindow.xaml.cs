using ProxyClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using StreamReader = ProxyClasses.StreamReader;

namespace ProxyServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ProxyserverWindow : Window
    {
        ObservableCollection<HttpRequest> LogItems = new ObservableCollection<HttpRequest>();
        Cacher cacher = new Cacher();
        readonly StreamReader streamReader = new StreamReader();
        TcpListener tcpListner;
        ProxySettingsViewModel settings;
        HttpRequest clientRequest;
        HttpRequest cachedResponseObject;
        HttpRequest proxyResponse;
        CacheItem cachedResponse;
        TcpClient tcpClient;
        byte[] responseData;
        object _itemsLock = new object();
        delegate void updateUIDelegate(HttpRequest httpRequest);
        static updateUIDelegate updateUIWithDelegate;
        public ProxyserverWindow()
        {
            InitializeComponent();
            settings = new ProxySettingsViewModel
            {
                Port = 8090,
                CacheTimeout = 60000,
                BufferSize = 200,
                CheckModifiedContent = true,
                ContentFilterOn = true,
                BasicAuthOn = false,
                AllowChangeHeaders = true,
                LogRequestHeaders = false,
                LogContentIn = true,
                LogContentOut = true,
                LogCLientInfo = true,
                ServerRunning = false
            };
            BindingOperations.EnableCollectionSynchronization(LogItems, _itemsLock);
            updateUIWithDelegate = new updateUIDelegate(UpdateUIWithLogItem);
            // set the binding
            settingsBlock.DataContext = settings;
            logListBox.ItemsSource = LogItems;
            updateUIWithDelegate(new HttpRequest(HttpRequest.MESSAGE, settings)
            {
                LogItemInfo = "Log van:\r\n" +
                "   * request headers\r\n" +
                "   * Response headers\r\n" +
                "   * content in\r\n" +
                "   * content uit\r\n" +
                "   * Client (Which browser is connected)"
            });
        }
        private async void BtnStartStopProxy_Click(object sender, RoutedEventArgs e)
        {
            // stop server if running
            if (settings.ServerRunning)
            {
                StopProxyServer();
                return;
            }
            tcpListner = new TcpListener(IPAddress.Any, settings.Port);
            tcpListner.Start();
            UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Listening for HTTP REQUEST" });
            settings.ServerRunning = true;
            while (true)
            {
                tcpClient = await tcpListner.AcceptTcpClientAsync();
                //Task t = Task.Run(async () => await ListenForHttpRequest(tcpClient));
                await ListenForHttpRequest(tcpClient);
            }
        }
        private void UpdateUIWithLogItem(HttpRequest logItem)
        {
            LogItems.Add(logItem);
        }

        private async Task ListenForHttpRequest(TcpClient tcpClient)
        {
            try
            {
                NetworkStream clientStream = tcpClient.GetStream();
                byte[] requestBytes = await streamReader.GetBytesFromReading(settings.BufferSize, clientStream);
                string requestInfo = ASCIIEncoding.ASCII.GetString(requestBytes, 0, requestBytes.Length);

                // firefox spam requests
                if (!requestInfo.Contains("detectportal") && !requestInfo.Contains("pusher"))
                {
                    clientRequest = new HttpRequest(HttpRequest.REQUEST, settings) { LogItemInfo = requestInfo };
                    if (settings.LogContentIn) UpdateUIWithLogItem(clientRequest);
                    await HandleHttpRequest(clientStream);
                    await HandleProxyRequest(clientStream);
                }
            }
            catch (ObjectDisposedException)
            {
                UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Stream closed early!: \r\n " });
            }
            catch (ArgumentException err)
            {
                UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Argument Exception!: \r\n " + err.Message });
            }
            catch (NullReferenceException err)
            {
            }
            catch (UriFormatException err)
            {
                UpdateUIWithLogItem(new HttpRequest(HttpRequest.ERROR, settings) { LogItemInfo = $"Bad request from {clientRequest.Method} ERROR:\r\n {err.Message}" });
            }
            catch (SocketException)
            {
                UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Unable to find host" });
            }
            catch (IOException err)
            {
                UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Stream closed: \r\n " + err.Message });
            }
            catch (BadRequestException err)
            {
                UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "BAd Request. LOG: \r\n " + err.Message });
            }
        }
        private async Task HandleHttpRequest(NetworkStream clientStream)
        {
            if (settings.BasicAuthOn && !await DoBasicAuth(clientStream)) throw new BadRequestException("User is not autherised!");
            if (cacher.RequestKnown(clientRequest.Method))
            {
                cachedResponse = cacher.GetKnownResponse(clientRequest.Method);
                bool olderThanTimeout = cacher.OlderThanTimeout(cachedResponse, settings.CacheTimeout);
                if (olderThanTimeout)
                {
                    cacher.RemoveItem(clientRequest.Method);
                    return;
                }
                byte[] knownResponseBytes = cachedResponse.ResponseBytes;
                if (settings.ContentFilterOn) knownResponseBytes = await streamReader.ReplaceImages(knownResponseBytes);
                string knownResponse = Encoding.ASCII.GetString(knownResponseBytes, 0, knownResponseBytes.Length);
                cachedResponseObject = new HttpRequest(HttpRequest.CACHED_RESPONSE, settings) { LogItemInfo = knownResponse };
                UpdateUIWithLogItem(cachedResponseObject);
                string modifiedDate = cachedResponseObject.GetHeader("Last-Modified");
                if (modifiedDate != "" && settings.CheckModifiedContent) clientRequest.UpdateHeader("If-Modified-Since", $" {modifiedDate}");
            }
        }
        private async Task HandleProxyRequest(NetworkStream clientStream)
        {
            responseData = await streamReader.MakeProxyRequestAsync(clientRequest, settings.BufferSize);
            string responseString = Encoding.ASCII.GetString(responseData, 0, responseData.Length);
            proxyResponse = new HttpRequest(HttpRequest.RESPONSE, settings) { LogItemInfo = responseString };
            if (proxyResponse.Method.Contains("304 Not Modified"))
            {
                responseData = cachedResponse.ResponseBytes;
            }
            if (settings.ContentFilterOn) responseData = await streamReader.ReplaceImages(responseData);
            UpdateUIWithLogItem(proxyResponse);
            await streamReader.WriteMessageWithBufferAsync(clientStream, responseData, settings.BufferSize);
            OnEndRequest(clientStream);
        }

        private void OnEndRequest(NetworkStream clientStream)
        {
            //Do not save img or partial content
            if (!proxyResponse.GetHeader("Content-Type").Contains("image")
                && proxyResponse.Method.Contains("200 OK")) cacher.addRequest(clientRequest.Method, responseData);
            tcpClient.Dispose();
            clientStream.Dispose();
        }

        private async Task<bool> DoBasicAuth(NetworkStream clientStream)
        {
            string authHeader = clientRequest.GetHeader("Authorization");
            if (authHeader == "")
            {
                await SendUnAutherizedResponse(clientStream);
                return false;
            }
            string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
            if (usernamePassword != "admin:admin")
            {
                await SendUnAutherizedResponse(clientStream);
                return false;
            }
            return true;
        }
        public async Task SendUnAutherizedResponse(NetworkStream clientStream)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("HTTP/1.1 401 Unauthorized");
            builder.AppendLine($"Date: {DateTime.Now}");
            builder.AppendLine();
            builder.AppendLine($"<html><body><h1>Unauthorized</h1></body></html>");
            builder.AppendLine();
            byte[] badRequestResponse = Encoding.ASCII.GetBytes(builder.ToString());
            await streamReader.WriteMessageWithBufferAsync(clientStream, badRequestResponse, settings.BufferSize);
            proxyResponse = new HttpRequest(HttpRequest.RESPONSE, settings) { LogItemInfo = builder.ToString() };
            tcpClient.Close();
            clientStream.Close();
            clientRequest = null;
            proxyResponse = null;
            cachedResponse = null;
        }
        private void StopProxyServer()
        {
            if (tcpListner != null)
                UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Stopping proxy Server..." });
            tcpListner.Stop();
            settings.ServerRunning = false;
            UpdateUIWithLogItem(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Proxy server Stopped Running" });
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LogItems.Clear();
        }
        public async Task SendBadRequest(NetworkStream clientStream)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("HTTP/1.1 400 Bad Request");
            builder.AppendLine($"Date: {DateTime.Now}");
            builder.AppendLine();
            builder.AppendLine("<html><body><h1>Bad Request</h1></body></html>");
            builder.AppendLine();
            byte[] badRequestResponse = Encoding.ASCII.GetBytes(builder.ToString());
            await streamReader.WriteMessageWithBufferAsync(clientStream, badRequestResponse, settings.BufferSize);
            proxyResponse = new HttpRequest(HttpRequest.RESPONSE, settings) { LogItemInfo = builder.ToString() };
        }
    }
}
