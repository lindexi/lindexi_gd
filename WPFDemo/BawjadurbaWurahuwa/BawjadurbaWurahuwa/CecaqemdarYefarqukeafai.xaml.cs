using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace BawjadurbaWurahuwa;

/// <summary>
/// CecaqemdarYefarqukeafai.xaml 的交互逻辑
/// </summary>
public partial class CecaqemdarYefarqukeafai : UserControl
{
    public CecaqemdarYefarqukeafai()
    {
        InitializeComponent();
    }

    public ObservableCollection<int> Collection { get; } = new ObservableCollection<int>();

    public event EventHandler<CecaqemdarYefarqukeafai>? Click;

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, this);
    }
}
