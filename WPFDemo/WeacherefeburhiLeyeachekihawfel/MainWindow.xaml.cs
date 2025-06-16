using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using TouchSocket.Sockets;

namespace WeacherefeburhiLeyeachekihawfel;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var originVideoUrl = new Uri("http://172.20.114.23:51779/Video.mp4");
        originVideoUrl = new Uri("https://file-examples.com/wp-content/storage/2017/04/file_example_MP4_480_1_5MG.mp4");

        var foo = new Foo(originVideoUrl);
        var downloadUrl = foo.Start();

        MediaElement.Source = new Uri(downloadUrl);
        //MediaElement.Source = originVideoUrl;
        MediaElement.LoadedBehavior = MediaState.Manual;
        MediaElement.Play();
    }

    class Foo
    {
        public Foo(Uri originVideoUrl)
        {
            _originVideoUrl = originVideoUrl;
        }

        private readonly Uri _originVideoUrl;

        public string Start()
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen(1);
            var ipEndPoint = (IPEndPoint) socket.LocalEndPoint!;
            var port = ipEndPoint.Port;

            Task.Run(async () =>
            {
                Socket s = await socket.AcceptAsync();
                socket.Dispose();
                if (ReferenceEquals(s, socket))
                {

                }

                using var networkStream = new NetworkStream(s);
                var buffer = new byte[1024 * 1024]; // 1 MB buffer
                var readLength = await networkStream.ReadAsync(buffer);

                var text = Encoding.UTF8.GetString(buffer, 0, readLength);
                /*
                   正常收到的 text 的内容大概如下
                   GET / HTTP/1.1
                   Accept: * /*
                   User-Agent: Windows-Media-Player/12.0.26100.4202
                   UA-CPU: AMD64
                   Accept-Encoding: gzip, deflate
                   Host: 127.0.0.1:62424
                   Connection: Keep-Alive


                 */

                using var socketsHttpHandler = new SocketsHttpHandler();
                socketsHttpHandler.SslOptions = new SslClientAuthenticationOptions()
                {
                    RemoteCertificateValidationCallback = delegate
                    {
                        // 这里可以验证证书，或者直接返回 true 跳过验证
                        return true;
                    }
                };
                using var httpClient = new HttpClient(socketsHttpHandler);

                using var httpResponseMessage = await httpClient.GetAsync(_originVideoUrl, HttpCompletionOption.ResponseHeadersRead);
                var contentLength = httpResponseMessage.Content.Headers.ContentLength ?? 0;

                var responseText = $"""
                                    HTTP/1.1 200 OK
                                    Content-Length: {contentLength}
                                    Content-Type: video/mp4
                                    """.Replace("\r\n", "\n") + "\r\n\r\n";
                var responseBytes = Encoding.ASCII.GetBytes(responseText);

                await networkStream.WriteAsync(responseBytes);
                await networkStream.FlushAsync();

                using var stream = await httpResponseMessage.Content.ReadAsStreamAsync();
                while (true)
                {
                    readLength = await stream.ReadAsync(buffer);
                    if (readLength < 0)
                    {
                        break;
                    }

                    try
                    {
                        await networkStream.WriteAsync(buffer.AsMemory(0, readLength));
                    }
                    catch (SocketException socketException)
                    {
                        break;
                    }
                }
            });
            return $"http://127.0.0.1:{port}";
        }
    }

    private void MediaElement_OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        MessageBox.Show($"Media failed: {e.ErrorException.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}