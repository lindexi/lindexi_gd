﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace LuwhiyailejawharKeafernereje
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SynchronizationContext.SetSynchronizationContext(null);
            Fx().Wait();
        }

        private async Task Fx()
        {
            await Task.Delay(100);
        }
    }

    class TaskHelper
    {
        public static void WaitAsync(Func<Task> func)
        {
            Task.Run(() => func().Wait()).Wait();
        }
    }
}
