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
using VirtualFileExplorer.Core;

namespace VirtualFileExplorer.Views;

/// <summary>
/// FileExplorerUserControl.xaml 的交互逻辑
/// </summary>
public partial class FileExplorerUserControl : UserControl
{
    public FileExplorerUserControl()
    {
        InitializeComponent();
    }

    public VirtualFileManager? FileManager { get; set; }
}
