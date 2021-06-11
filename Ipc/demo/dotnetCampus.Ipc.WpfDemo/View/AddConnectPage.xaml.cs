using System;
using System.Collections.Generic;
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
    /// AddConnectPage.xaml 的交互逻辑
    /// </summary>
    public partial class AddConnectPage : UserControl
    {
        public AddConnectPage()
        {
            InitializeComponent();
            BuildServerName();
        }

        private void ConnectServerButton_OnClick(object sender, RoutedEventArgs e)
        {
            ServerConnecting?.Invoke(this, ServerNameTextBox.Text);
        }

        private void StartServerButton_OnClick(object sender, RoutedEventArgs e)
        {
            ServerStarting?.Invoke(this, ServerNameTextBox.Text);
            BuildServerName();
        }

        private void BuildServerName()
        {
            ServerNameTextBox.Text = System.IO.Path.GetRandomFileName();
        }

        public event EventHandler<string>? ServerConnecting;

        public event EventHandler<string>? ServerStarting;
    }
}
