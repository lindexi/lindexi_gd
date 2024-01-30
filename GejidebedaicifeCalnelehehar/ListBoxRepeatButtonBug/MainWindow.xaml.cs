using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ListBoxRepeatButtonBug
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (GetChild(ListBox, o => o is ScrollViewer) is ScrollViewer scrollViewer)
            {
                scrollViewer.IsManipulationEnabled = false;
            }
        }

        private object? GetChild(DependencyObject root, Func<object, bool> predicate)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root,i);
                if (predicate(child))
                {
                    return child;
                }
                else if (child is DependencyObject dependencyObject)
                {
                    var result = GetChild(dependencyObject, predicate);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private void StackPanelRepeatButtonClick(object sender, RoutedEventArgs e)
            => StackPanelTextBlock.Text += "|";
        private void ListBoxRepeatButtonClick(object sender, RoutedEventArgs e)
            => ListBoxTextBlock.Text += "|";
    }
}
