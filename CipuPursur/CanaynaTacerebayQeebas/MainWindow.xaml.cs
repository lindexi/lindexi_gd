using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using lindexi.src;

namespace CanaynaTacerebayQeebas
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = (ViewModel) DataContext;
            ViewModel.OnNavigatedTo(this, null);

            Application.Current.Exit += (s, e) => { ViewModel.OnNavigatedFrom(this, null); };
        }

        private ViewModel ViewModel { set; get; }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Send();
        }

        private async void Send()
        {
            if (string.IsNullOrEmpty(ViewModel.Name) || string.IsNullOrEmpty(ViewModel.Code) ||
                string.IsNullOrEmpty(ViewModel.Text))
            {
                return;
            }

            var qpush = new Qpush(ViewModel.Name, ViewModel.Code);

            try
            {
                await qpush.PushMessageAsync(ViewModel.Text);
            }
            catch (HttpRequestException)
            {
            }

            ViewModel.Text = "";
        }

        private void Text_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                {
                    Send();
                }
            }
        }
    }
}