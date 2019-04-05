using ProxyClasses;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace ProxyServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ProxyserverWindow : Window
    {
        readonly ObservableCollection<HttpRequest> LogItems = new ObservableCollection<HttpRequest>();
        readonly ProxySettingsViewModel settings;
        readonly ProxyClasses.ProxyServer proxyServer;
        readonly Logger logger;
        public ProxyserverWindow()
        {
            InitializeComponent();
            settings = new ProxySettingsViewModel();
            logger = new Logger(LogItems, settings);
            proxyServer = new ProxyClasses.ProxyServer(settings);
            // set the binding
            settingsBlock.DataContext = settings;
            logListBox.ItemsSource = LogItems;
            //welcome message
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
            while (true)
            {
                await TryAcceptingClients();
            }
        }

        private async Task TryAcceptingClients()
        {
            try
            {
                await proxyServer.AcceptTcpClientAsync(logger);
            }
            catch (UriFormatException)
            {
                logger.Log(new HttpRequest(HttpRequest.ERROR) { LogItemInfo = $"Invalid hostname!" });
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
                logger.Log(new HttpRequest(HttpRequest.ERROR) { LogItemInfo = "Unable to find host" });
            }
            catch (IOException err)
            {
                logger.Log(new HttpRequest(HttpRequest.ERROR) { LogItemInfo = "Stream closed: \r\n " + err.Message });
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
