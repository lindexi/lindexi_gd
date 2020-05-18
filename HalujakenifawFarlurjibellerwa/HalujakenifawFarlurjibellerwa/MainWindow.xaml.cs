using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;

namespace HalujakenifawFarlurjibellerwa
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

        private void SentimentAnalysis_OnClick(object sender, RoutedEventArgs e)
        {
            var client = GetAnalyticsClient();

            var sentiment = client.Sentiment(Text.Text, "zh");
            ShowText.Text = $"分数:{sentiment.Score:0.00} \r\n 评分接近 0 表示消极情绪，评分接近 1 表示积极情绪";
        }

        private void LanguageDetection_OnClick(object sender, RoutedEventArgs e)
        {
            var client = GetAnalyticsClient();
            var detectLanguage = client.DetectLanguage(Text.Text);
            ShowText.Text =
                $"判断出可能的语言有 {detectLanguage.DetectedLanguages.Count} 个 \r\n {string.Join("\r\n", detectLanguage.DetectedLanguages.Select(temp => $"语言 {temp.Name} 分数 {temp.Score:0.00}"))}";
        }

        private static TextAnalyticsClient GetAnalyticsClient()
        {
            var key = "d131f725093f460c99a09580beba34ed";
            var endpoint = "https://lindexi.cognitiveservices.azure.cn/";

            var credentials = new ApiKeyServiceClientCredentials(key);
            TextAnalyticsClient client = new TextAnalyticsClient(credentials)
            {
                Endpoint = endpoint
            };

            return client;
        }

        private void RecognizeEntities_OnClick(object sender, RoutedEventArgs e)
        {
            var client = GetAnalyticsClient();
            var result = client.Entities(Text.Text);
            ShowText.Text = "";
            foreach (var entity in result.Entities)
            {
                ShowText.Text +=
                    $"Name: {entity.Name},\tType: {entity.Type ?? "N/A"},\tSub-Type: {entity.SubType ?? "N/A"} \r\n";
                foreach (var match in entity.Matches)
                {
                    ShowText.Text +=
                        $"\tOffset: {match.Offset},\tLength: {match.Length},\tScore: {match.EntityTypeScore:F3}\r\n";
                }
            }
        }

        private void KeyPhraseExtraction_OnClick(object sender, RoutedEventArgs e)
        {
            var client = GetAnalyticsClient();
            var result = client.KeyPhrases(Text.Text);
            ShowText.Text = $"关键词： {string.Join(";", result.KeyPhrases)}";
        }
    }

    class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        public ApiKeyServiceClientCredentials(string apiKey)
        {
            _apiKey = apiKey;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        private readonly string _apiKey;
    }
}