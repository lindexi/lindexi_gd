using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        }

        public ConnectedPeerModel ConnectedPeerModel { set; get; } = null!;

        public string ServerName { set; get; } = null!;

        private async void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            ConnectedPeerModel.AddMessage(ServerName, MessageTextBox.Text);
            await ConnectedPeerModel.Peer.IpcMessageWriter.WriteMessageAsync(MessageTextBox.Text, "CharPage").ConfigureAwait(false);
        }
    }

    public static class ListViewExtensions
    {
        public static void ScrollToBottom(this ListView listView)
        {
            DependencyObject border = VisualTreeHelper.GetChild(listView, 0);
            ScrollViewer scrollViewer = (ScrollViewer) VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }
    }
}
