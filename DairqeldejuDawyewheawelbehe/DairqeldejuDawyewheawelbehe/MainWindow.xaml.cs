using Microsoft.Recognizers.Text;
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
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text.Choice;
using Microsoft.Recognizers.Text.Sequence;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.NumberWithUnit;

namespace DairqeldejuDawyewheawelbehe
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

        private static string ModelResultToString(List<ModelResult> list)
        {
            var pre = "";
            var breakLine = "\r\n";
            var str = new StringBuilder();
            foreach (var modelResult in list)
            {
                str.Append(pre)
                    .Append("关键词： ")
                    .Append(modelResult.Text)
                    .Append(breakLine)
                    .Append(pre)
                    .Append($"起点 {modelResult.Start} 终点 {modelResult.End}")
                    .Append(breakLine);
                if (modelResult.Resolution.TryGetValue("value", out var value))
                {
                    str.Append(pre)
                        .Append("值：")
                        .Append(value)
                        .Append(breakLine);
                }

                str.Append(breakLine);
            }

            return str.ToString();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var text = Text.Text;
            var modelInfoList = new List<ModelInfo>();

            RecognizeNumber(text, modelInfoList);
            RecognizeOrdinal(text, modelInfoList);

            RecognizeAge(text, modelInfoList);
            RecognizeCurrency(text, modelInfoList);
            RecognizeDimension(text, modelInfoList);
            RecognizeTemperature(text, modelInfoList);

            RecognizeDateTime(text, modelInfoList);

            RecognizePhoneNumber(text, modelInfoList);
            RecognizeIpAddress(text, modelInfoList);
            RecognizeUrl(text, modelInfoList);

            RecognizeBoolean(text, modelInfoList);

            ListView.ItemsSource = modelInfoList;
        }

        private void RecognizeBoolean(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeBoolean = ChoiceRecognizer.RecognizeBoolean(text, Culture.Chinese);

            if (recognizeBoolean.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("布尔", ModelResultToString(recognizeBoolean)));
            }
        }

        private void RecognizeIpAddress(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeIpAddress = SequenceRecognizer.RecognizeIpAddress(text, Culture.Chinese);

            if (recognizeIpAddress.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("IP", ModelResultToString(recognizeIpAddress)));
            }
        }

        private void RecognizeUrl(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeUrl = SequenceRecognizer.RecognizeURL(text, Culture.Chinese);

            if (recognizeUrl.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("Url", ModelResultToString(recognizeUrl)));
            }
        }

        private void RecognizePhoneNumber(string text, List<ModelInfo> modelInfoList)
        {
            var recognizePhoneNumber = SequenceRecognizer.RecognizePhoneNumber(text, Culture.Chinese);

            if (recognizePhoneNumber.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("电话号", ModelResultToString(recognizePhoneNumber)));
            }
        }

        private void RecognizeDateTime(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeDateTime = DateTimeRecognizer.RecognizeDateTime(text, Culture.Chinese);

            if (recognizeDateTime.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("时间", ModelResultToString(recognizeDateTime)));
            }
        }

        private void RecognizeTemperature(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeTemperature = NumberWithUnitRecognizer.RecognizeTemperature(text, Culture.Chinese);

            if (recognizeTemperature.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("温度", ModelResultToString(recognizeTemperature)));
            }
        }

        private void RecognizeDimension(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeDimension = NumberWithUnitRecognizer.RecognizeDimension(text, Culture.Chinese);

            if (recognizeDimension.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("大小", ModelResultToString(recognizeDimension)));
            }
        }

        private void RecognizeCurrency(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeCurrency = NumberWithUnitRecognizer.RecognizeCurrency(text, Culture.Chinese);

            if (recognizeCurrency.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("货币", ModelResultToString(recognizeCurrency)));
            }
        }

        private void RecognizeAge(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeAge = NumberWithUnitRecognizer.RecognizeAge(text, Culture.Chinese);

            if (recognizeAge.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("年龄", ModelResultToString(recognizeAge)));
            }
        }

        private void RecognizeOrdinal(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeOrdinal = NumberRecognizer.RecognizeOrdinal(text, Culture.Chinese);
            if (recognizeOrdinal.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("序号", ModelResultToString(recognizeOrdinal)));
            }
        }

        private void RecognizeNumber(string text, List<ModelInfo> modelInfoList)
        {
            var recognizeNumber = NumberRecognizer.RecognizeNumber(text, Culture.Chinese);

            if (recognizeNumber.Count > 0)
            {
                modelInfoList.Add(new ModelInfo("数值", ModelResultToString(recognizeNumber)));
            }
        }
    }

    public class ModelInfo
    {
        public ModelInfo(string title, string content)
        {
            Title = title;
            Content = content;
        }

        public string Title { get; }

        public string Content { get; }
    }
}