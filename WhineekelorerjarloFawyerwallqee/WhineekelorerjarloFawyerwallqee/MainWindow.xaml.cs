using Microsoft.Maui.Controls;

using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Button = Microsoft.Maui.Controls.Button;
using Page = Microsoft.Maui.Controls.Page;
using Window = System.Windows.Window;

namespace WhineekelorerjarloFawyerwallqee;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WindowHandler.Mapper.Add(nameof(IWindow.Title), (handler, window1) =>
        {

        });

        ContentViewHandler.Mapper.Add(nameof(IContentView.Content), MapContent);

      
    }
}

