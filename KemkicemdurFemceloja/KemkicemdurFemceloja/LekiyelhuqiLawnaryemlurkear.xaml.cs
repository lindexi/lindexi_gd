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

namespace KemkicemdurFemceloja
{
    /// <summary>
    /// LekiyelhuqiLawnaryemlurkear.xaml 的交互逻辑
    /// </summary>
    public partial class LekiyelhuqiLawnaryemlurkear : UserControl
    {
        public LekiyelhuqiLawnaryemlurkear()
        {
            Loaded += LekiyelhuqiLawnaryemlurkear_Loaded;
        }

        private void LekiyelhuqiLawnaryemlurkear_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeComponent();
        }
    }
}
