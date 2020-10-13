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

namespace dotnetCampus.Ipc.WpfDemo
{
    /// <summary>
    /// ServerPage.xaml 的交互逻辑
    /// </summary>
    public partial class ServerPage : UserControl
    {
        public ServerPage()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ServerNameProperty = DependencyProperty.Register(
            "ServerName", typeof(string), typeof(ServerPage), new PropertyMetadata("dotnet campus"));

        public string ServerName
        {
            get { return (string) GetValue(ServerNameProperty); }
            set { SetValue(ServerNameProperty, value); }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            OnServerStarting(ServerName);
            var button = (Button) sender;
            button.IsEnabled = false;
            ServerNameTextBox.IsReadOnly = true;
        }

        public event EventHandler<string> ServerStarting;

        protected virtual void OnServerStarting(string e)
        {
            ServerStarting?.Invoke(this, e);
        }
    }
}
