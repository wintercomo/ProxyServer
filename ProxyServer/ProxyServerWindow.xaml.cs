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
        ProxySettingsViewModel settings;

        ProxyClasses.ProxyServer proxyServer;
        Logger logger;
        public ProxyserverWindow()
        {
            InitializeComponent();
            logger = new Logger(LogItems);
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
            proxyServer = new ProxyClasses.ProxyServer(settings);
            // set the binding
            settingsBlock.DataContext = settings;
            logListBox.ItemsSource = LogItems;
            logger.Log(new HttpRequest()
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
                StopServer();
                return;
            }
            proxyServer.StartServer();
            logger.Log(new HttpRequest() { LogItemInfo = "Listening for http request on TCP level" });
            settings.ServerRunning = true;
            btnStartStopProxy.Content = "STOP";
            try
            {
                while (true)
                {
                    await proxyServer.AcceptTcpClientAsync(logger);
                }
            }

            catch (ObjectDisposedException)
            {
                logger.Log(new HttpRequest(HttpRequest.ERROR, settings) { LogItemInfo = "Server stopped" });
            }

            catch (ArgumentException err)
            {
                logger.Log(new HttpRequest(HttpRequest.ERROR, settings) { LogItemInfo = "Argument Exception!: \r\n " + err.Message });
            }
            catch (UriFormatException err)
            {
                logger.Log(new HttpRequest(HttpRequest.ERROR, settings) { LogItemInfo = $"Bad request from ERROR:\r\n {err.Message}" });
            }
            catch (SocketException)
            {
                logger.Log(new HttpRequest(HttpRequest.ERROR, settings) { LogItemInfo = "Unable to find host" });
            }
            catch (IOException err)
            {
                logger.Log(new HttpRequest(HttpRequest.MESSAGE, settings) { LogItemInfo = "Stream closed: \r\n " + err.Message });
            }
            finally
            {
                StopServer();
            }
        }

        private void StopServer()
        {
            proxyServer.StopServer();
            settings.ServerRunning = false;
            btnStartStopProxy.Content = "Start Server";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LogItems.Clear();
        }
    }
}
