using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NewhawhebichaJalciceerulebaiwair
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        public ViewModel ViewModel { get; } = new ViewModel();

        private void TextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(((UIElement) sender).Focus);
        }
    }

    public class ViewModel
    {
        public ICommand Command { get; } = new Command();
    }

    public class Command : ICommand
    {
        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <inheritdoc />
        public void Execute(object parameter)
        {
            Debug.WriteLine("林德熙是逗比");
        }

        /// <inheritdoc />
        public event EventHandler CanExecuteChanged;
    }
}
