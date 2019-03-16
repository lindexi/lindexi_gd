using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace KeejemairbouLirallpurpallnasfakaw
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Task.Run(async () =>
            {
                while (true)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => { Orientation = Orientation.Horizontal; });

                    await Task.Delay(TimeSpan.FromSeconds(5));

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () => { Orientation = Orientation.Vertical; });

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            });
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(MainPage), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
    }

    public class BindingHelper
    {
        public static readonly DependencyProperty ItemsPanelOrientationProperty = DependencyProperty.RegisterAttached(
            "ItemsPanelOrientation", typeof(bool), typeof(BindingHelper),
            new PropertyMetadata(default(bool), ItemsPanelOrientation_OnPropertyChanged));

        private static async void ItemsPanelOrientation_OnPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                await listView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (listView.ItemsPanelRoot is ItemsStackPanel stackPanel)
                    {
                        BindingOperations.SetBinding(stackPanel, ItemsStackPanel.OrientationProperty, new Binding()
                        {
                            Path = new PropertyPath("Orientation"),
                            Mode = BindingMode.OneWay
                        });
                    }
                });
            }
        }

        public static void SetItemsPanelOrientation(DependencyObject element, bool value)
        {
            element.SetValue(ItemsPanelOrientationProperty, value);
        }

        public static bool GetItemsPanelOrientation(DependencyObject element)
        {
            return (bool) element.GetValue(ItemsPanelOrientationProperty);
        }
    }
}