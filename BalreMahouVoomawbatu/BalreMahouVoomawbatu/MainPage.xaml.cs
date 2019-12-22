using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graph;
using Microsoft.Identity.Client;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace BalreMahouVoomawbatu
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 欢迎访问我博客 http://lindexi.gitee.io 里面有大量 UWP WPF 博客

            var provider = new DelegateAuthenticationProvider(AuthenticateRequestAsyncDelegate);

            var client = new GraphServiceClient(provider);

            await client.Me.SendMail(new Message()
            {
                Subject = "调用Microsoft Graph发出的邮件",
                Body = new ItemBody()
                {
                    ContentType = BodyType.Text,
                    Content = "这是一封调用了Microsoft Graph服务发出的邮件，范例参考 https://github.com/chenxizhang/office365dev"
                },
                ToRecipients = new[]
                {
                    new Recipient()
                    {
                        EmailAddress = new EmailAddress() {Address = "lindexi_gd@outlook.com"}
                    }
                }
            }, SaveToSentItems: true /*保存到发送邮件夹*/).Request().PostAsync();
        }

        private async Task AuthenticateRequestAsyncDelegate(HttpRequestMessage request)
        {

            string clientID = "2f56798a-66f7-4330-9bc4-d3a8a0898642"; //这个ID是我创建的一个临时App的ID，请替换为自己的
            string[] scopes = { "User.Read", "Mail.Read", "Mail.Send", "Files.Read" };

            var clientApplication = new PublicClientApplication(clientID);

            var authenticationResult = await clientApplication.AcquireTokenAsync(scopes);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
        }
    }
}
