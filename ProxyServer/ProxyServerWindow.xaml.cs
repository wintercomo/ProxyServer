using ProxyClasses;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

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
            // allow updates from different threads
            object _itemsLock = new object();
            BindingOperations.EnableCollectionSynchronization(LogItems, _itemsLock);

            // set the binding
            logger = new Logger(LogItems, settings, _itemsLock);
            proxyServer = new ProxyClasses.ProxyServer(settings);
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
            while (true) await TryAcceptingClients();
        }

        private async Task TryAcceptingClients()
        {
            try
            {
                await Task.Run(() => proxyServer.AcceptTcpClientAsync(logger));
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
            }catch (InvalidOperationException err)
            {
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
