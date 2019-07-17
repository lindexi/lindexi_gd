using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Babukeelleneeoai
{
    public partial class ConvertFileEncodingPage : Window
    {
        public ConvertFileEncodingPage(FileInfo file)
        {
            if (!file.Exists)
            {
                throw new ArgumentException($"文件{file}找不到");
            }

            ViewModel = new ConvertFileEncodingModel()
            {
                File = file
            };

            DataContext = ViewModel;

            InitializeComponent();

            CurrentFile.Text = "CurrentFile: " + ViewModel.File.FullName;
        }

        public ConvertFileEncodingModel ViewModel { get; }

        private void ConvertFile_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ConvertFile())
            {
                var storyboard = (Storyboard) FindResource("SuccessStoryboard");
                storyboard.Completed += async (o, args) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    Close();
                };
                storyboard.Begin();
            }
            else
            {
                var storyboard = (Storyboard) FindResource("FailStoryboard");
                storyboard.Begin();
            }
        }
    }
}