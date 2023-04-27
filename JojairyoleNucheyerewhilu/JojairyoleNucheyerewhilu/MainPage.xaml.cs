using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Data.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace JojairyoleNucheyerewhilu
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var inputText = InputTextBox.Text;
            if (string.IsNullOrEmpty(inputText))
            {
                return;
            }

            var stringBuilder = new StringBuilder();

            var wordsSegmenter = new WordsSegmenter("zh-CN");
            var wordSegmentList = wordsSegmenter.GetTokens(inputText);
            stringBuilder.AppendLine($"单词数量：{wordSegmentList.Count}");

            for (var i = 0; i < wordSegmentList.Count; i++)
            {
                var wordSegment = wordSegmentList[i];
                stringBuilder.AppendLine($"[{i}] Start:{wordSegment.SourceTextSegment.StartPosition};Length={wordSegment.SourceTextSegment.Length} {wordSegment.Text}");
            }

            TextBox.Text = stringBuilder.ToString();
        }
    }
}
