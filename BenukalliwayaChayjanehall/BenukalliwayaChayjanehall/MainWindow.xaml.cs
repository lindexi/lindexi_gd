using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Windows.AI.MachineLearning;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage.Streams;

namespace BenukalliwayaChayjanehall;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        LearningModel learningModel = LearningModel.LoadFromFilePath("mnist.onnx");
        var deviceToRunOn = new LearningModelDevice(LearningModelDeviceKind.DirectXHighPerformance);
        var learningModelSession = new LearningModelSession(learningModel, deviceToRunOn);
        var learningModelBinding = new LearningModelBinding(learningModelSession);

        LearningModel = learningModel;
        LearningModelSession = learningModelSession;
        LearningModelBinding = learningModelBinding;
    }

    public LearningModel LearningModel { get; set; }

    public LearningModelSession LearningModelSession { get; set; }

    public LearningModelBinding LearningModelBinding { get; set; }

    private async void RecognizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        var width = (int) InkCanvas.ActualWidth;
        var height = (int) InkCanvas.ActualHeight;

        var bitmapSource = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmapSource.Render(InkCanvas);

        var length = bitmapSource.PixelWidth * bitmapSource.PixelHeight * bitmapSource.Format.BitsPerPixel / 8;
        var byteArray = new byte[length];
        var stride = bitmapSource.PixelWidth * bitmapSource.Format.BitsPerPixel / 8;
        bitmapSource.CopyPixels(byteArray, stride, 0);

        IBuffer buffer = byteArray.AsBuffer();

        var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(buffer, BitmapPixelFormat.Bgra8, bitmapSource.PixelWidth, bitmapSource.PixelHeight);
       
        VideoFrame inputImage = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);

        var imageFeatureValue = ImageFeatureValue.CreateFromVideoFrame(inputImage);

        LearningModelBinding.Bind("Input3", imageFeatureValue);

        var result = await LearningModelSession.EvaluateAsync(LearningModelBinding, "0");

        var resultOutput = result.Outputs["Plus214_Output_0"] as TensorFloat;
        var vectorView = resultOutput?.GetAsVectorView();
        if (vectorView != null)
        {
            var maxValue = 0f;
            var maxIndex = -1;
            // 10 个数字，每个数字
            for (var number = 0; number < vectorView.Count; number++)
            {
                Debug.WriteLine($"{number} {vectorView[number]}");

                if (vectorView[number] > maxValue)
                {
                    maxValue = vectorView[number];
                    maxIndex = number;
                }
            }

            if (maxIndex == -1)
            {
                TextBlock.Text = $"识别失败";
            }
            else
            {
                TextBlock.Text = $"识别数字：{maxIndex} 识别率：{maxValue}";
            }
        }
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        InkCanvas.Children.Clear();
        InkCanvas.Strokes.Clear();
    }
}