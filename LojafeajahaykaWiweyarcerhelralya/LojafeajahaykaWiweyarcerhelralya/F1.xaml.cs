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

namespace LojafeajahaykaWiweyarcerhelralya
{
    /// <summary>
    /// F1.xaml 的交互逻辑
    /// </summary>
    public partial class F1 : UserControl
    {
        public F1()
        {
            Loaded += F1_Loaded;
        }

        private void F1_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        public override void EndInit()
        {

            base.EndInit();
        }
    }
}
