using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace NurkagucemNairrebulaleki
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

        private void AddAndGetValue_OnClick(object sender, RoutedEventArgs e)
        {
            const int count = 10000000;

            var resourceDictionary = new ResourceDictionary();

            Task.Run(() =>
            {
                for (int i = 0; i < count / 2; i++)
                {
                    resourceDictionary.Add(i, i);
                }

                // 加入完成
                Debugger.Break();
            });

            Task.Run(() =>
            {
                for (int i = count / 2 + 1; i < count; i++)
                {
                    resourceDictionary.Add(i, i);
                }

                // 加入完成
                Debugger.Break();
            });

            Task.Run(() =>
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    _ = resourceDictionary[i];
                }
            });
        }

        private void AddAndGetValueWithCanBeAccessedAcrossThreads_OnClick(object sender, RoutedEventArgs e)
        {
            const int count = 10000000;

            var resourceDictionary = new ResourceDictionary();
            ResourceDictionaryCanBeAccessedAcrossThreadsHelper
                .SetCanBeAccessedAcrossThreads(resourceDictionary);

            Task.Run(() =>
            {
                for (int i = 0; i < count / 2; i++)
                {
                    resourceDictionary.Add(i, i);
                }

                // 加入完成
                Debugger.Break();
            });

            Task.Run(() =>
            {
                for (int i = count / 2 + 1; i < count; i++)
                {
                    resourceDictionary.Add(i, i);
                }

                // 加入完成
                Debugger.Break();
            });

            Task.Run(() =>
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    _ = resourceDictionary[i];
                }
            });
        }

        private void AddAndContains_OnClick(object sender, RoutedEventArgs e)
        {
            const int count = 10000000;

            var resourceDictionary = new ResourceDictionary();

            Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    resourceDictionary.Add(i, i);
                }
            });

            Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    resourceDictionary.Contains(i);
                }
            });
        }
    }

    public static class ResourceDictionaryCanBeAccessedAcrossThreadsHelper
    {
        public static void SetCanBeAccessedAcrossThreads(ResourceDictionary resourceDictionary)
        {
            _ = new InnerFrameworkTemplate
            {
                // 在 InnerFrameworkTemplate 的 Resources 属性里面
                // 将会设置 Resources.CanBeAccessedAcrossThreads = true 的值
                // 也就是让 Resources 的读写获取都加上锁
                Resources = resourceDictionary
            };
        }

        private class InnerFrameworkTemplate : DataTemplate
        {
        }
    }
}