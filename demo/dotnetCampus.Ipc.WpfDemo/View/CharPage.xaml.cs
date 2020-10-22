using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace dotnetCampus.Ipc.WpfDemo.View
{
    /// <summary>
    /// CharPage.xaml 的交互逻辑
    /// </summary>
    public partial class CharPage : UserControl
    {
        public CharPage()
        {
            InitializeComponent();

            Loaded += CharPage_Loaded;
        }

        private void CharPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.Assert(ConnectedPeerModel != null);

            DataContext = ConnectedPeerModel;
            MessageListView.ScrollToBottom();

            // 有消息过来，自动滚动到最下
            ConnectedPeerModel.MessageList.CollectionChanged += (o, args) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    MessageListView.ScrollToBottom();
                }, DispatcherPriority.Background);
            };
        }

        public ConnectedPeerModel ConnectedPeerModel { set; get; } = null!;

        public string ServerName { set; get; } = null!;

        private async void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            ConnectedPeerModel.AddMessage(ServerName, MessageTextBox.Text);
            await ConnectedPeerModel.Peer.IpcMessageWriter.WriteMessageAsync(MessageTextBox.Text, "CharPage").ConfigureAwait(false);
        }
    }
}
