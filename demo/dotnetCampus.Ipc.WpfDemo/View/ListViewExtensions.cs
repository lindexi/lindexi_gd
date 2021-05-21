using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace dotnetCampus.Ipc.WpfDemo.View
{
    public static class ListViewExtensions
    {
        public static void ScrollToBottom(this ListView listView)
        {
            DependencyObject border = VisualTreeHelper.GetChild(listView, 0);
            ScrollViewer scrollViewer = (ScrollViewer) VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }
    }
}
