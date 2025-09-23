using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Threading;

namespace RobeekaguryeekeaNemjahaykubel.Views;

public partial class MainView : UserControl
{
    private string[] _imageFileList;

    public MainView()
    {
        InitializeComponent();

        var imageFolder = @"C:\lindexi\ImageFolder\";
        Directory.CreateDirectory(imageFolder);
        var count = 0;
        foreach (var imageFile in Directory.EnumerateFiles(@"c:\lindexi\课件\", "*.png", SearchOption.AllDirectories))
        {
            count++;
            var newImageFile = Path.Combine(imageFolder, $"Image{count:00000}.png");
            //File.Copy(imageFile, newImageFile);
            File.CreateSymbolicLink(newImageFile, imageFile);
        }

        var imageFileList = Directory.GetFiles(imageFolder, "*.png", SearchOption.AllDirectories);
        _imageFileList = imageFileList;

        Loaded += MainView_Loaded;
    }

    private async void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        for (var i = 0; i < _imageFileList.Length; i++)
        {
            var imageFile = _imageFileList[i];
            try
            {
                RootImage.Source = new Bitmap(imageFile);
                Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                await Task.Delay(16);
            }
            catch (Exception exception)
            {
            }
        }
    }
}