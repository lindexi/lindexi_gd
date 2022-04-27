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

namespace NolawkurduKofurkacallka
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Task.Run(Visit);
        }

        private async Task Visit()
        {
            var random = new Random();
            while (true)
            {
                var listNode = LinkedList.First;
                var count = 0;
                while (listNode != null)
                {
                    var value = listNode.Value;
                    for (int i = 0; i < 100; i++)
                    {
                        var n = value[random.Next(value.Length)];
                        unchecked
                        {
                            value[random.Next(value.Length)] = (byte) (random.Next() + n);
                        }
                    }

                    count++;

                    if (count == 100)
                    {
                        await Task.Delay(500);
                        count = 0;
                    }

                    listNode = listNode.Next;
                }

                await Task.Delay(500);
            }
        }

        private LinkedList<byte[]> LinkedList { get; } = new LinkedList<byte[]>();

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            LinkedList.AddLast(new byte[1024000]);
            VisitLast();
        }

        private void Button2_OnClick(object sender, RoutedEventArgs e)
        {
            LinkedList.AddLast(new byte[1024000_00]);
            VisitLast();
        }

        private void Button3_OnClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                LinkedList.AddLast(new byte[1024000_00]);
                VisitLast();
            }
        }

        private void VisitLast()
        {
            var value = LinkedList.Last.Value;
            var random = new Random();
            for (int i = 0; i < value.Length; i += random.Next(1, 100))
            {
                value[i] = (byte)random.Next();
            }
        }
    }
}
