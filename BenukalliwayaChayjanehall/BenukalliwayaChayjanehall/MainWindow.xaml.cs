using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Windows.AI.MachineLearning;
using Windows.Graphics.Imaging;
using Windows.Media;

using BitmapDecoder = Windows.Graphics.Imaging.BitmapDecoder;

namespace BenukalliwayaChayjanehall;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LearningModel learningModel = LearningModel.LoadFromFilePath(@"F:\temp\classifier.onnx");
        var deviceToRunOn = new LearningModelDevice(LearningModelDeviceKind.DirectXHighPerformance);
        var learningModelSession = new LearningModelSession(learningModel, deviceToRunOn);
        var learningModelBinding = new LearningModelBinding(learningModelSession);

        for (int i = 0; i < 3; i++)
        {
            await using var fileStream = File.OpenRead($@"F:\temp\Image\{i}.jpg");

            var randomAccessStream = fileStream.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            VideoFrame inputImage = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);

            var imageFeatureValue = ImageFeatureValue.CreateFromVideoFrame(inputImage);

            learningModelBinding.Bind("data", imageFeatureValue);

            var result = await learningModelSession.EvaluateAsync(learningModelBinding, "0");
            var classLabel = result.Outputs["classLabel"] as TensorString;
            var loss = result.Outputs["loss"] as IList<IDictionary<string, float>>;

            Debug.WriteLine(classLabel);

            if (loss == null) return;
            foreach (var dictionary in loss)
            {
                foreach (var (key, value) in dictionary)
                {
                    Debug.WriteLine($"{key} {value}");
                }
            }
        }
    }
}