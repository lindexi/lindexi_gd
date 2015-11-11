using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace AutoSuggestBox_UWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            suggestions = new ObservableCollection<string>();
            this.InitializeComponent();
        }
        private ObservableCollection<String> suggestions;

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender , AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
                textBlock.Text = args.ChosenSuggestion.ToString();
            else
                textBlock.Text = sender.Text;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender , AutoSuggestBoxTextChangedEventArgs args)
        {
            suggestions.Clear();
            suggestions.Add(sender.Text + "1");
            suggestions.Add(sender.Text + "2");
            sender.ItemsSource = suggestions;
        }
    }
}
