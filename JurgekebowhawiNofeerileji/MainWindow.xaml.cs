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

namespace JurgekebowhawiNofeerileji
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public class GridExtensions
    {
        public static readonly DependencyProperty NameProperty = DependencyProperty.RegisterAttached(
            "Name", typeof(string), typeof(GridExtensions), new PropertyMetadata(default(string)));

        public static void SetName(DependencyObject element, string value)
        {
            element.SetValue(NameProperty, value);
        }

        public static string GetName(DependencyObject element)
        {
            return (string) element.GetValue(NameProperty);
        }

        public static readonly DependencyProperty RowNameProperty = DependencyProperty.RegisterAttached(
            "RowName", typeof(string), typeof(GridExtensions), new PropertyMetadata(default(string),RowName_PropertyChanged));

        private static void RowName_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement frameworkElement)
            {
                if (e.NewValue is string rowName)
                {
                    if (string.IsNullOrEmpty(rowName))
                    {
                        return;
                    }

                    if (frameworkElement.Parent is Grid grid)
                    {
                        for (var i = 0; i < grid.RowDefinitions.Count; i++)
                        {
                            var gridRowDefinition = grid.RowDefinitions[i];
                            var gridRowName = GetName(gridRowDefinition);
                            if (gridRowName.Equals(rowName, StringComparison.Ordinal))
                            {
                                Grid.SetRow(frameworkElement, i);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public static void SetRowName(DependencyObject element, string value)
        {
            element.SetValue(RowNameProperty, value);
        }

        public static string GetRowName(DependencyObject element)
        {
            return (string) element.GetValue(RowNameProperty);
        }
    }
}
