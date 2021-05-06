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

namespace JurchaichallwogalHeegukacealalbe
{
    /// <summary>
    /// TextControl.xaml 的交互逻辑
    /// </summary>
    public partial class TextControl : UserControl
    {
        public TextControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(TextControl));

        public string Text
        {
            get { return "lindexi is doubi"; }
            set { SetValue(TextProperty, value); }
        }
    }
}
