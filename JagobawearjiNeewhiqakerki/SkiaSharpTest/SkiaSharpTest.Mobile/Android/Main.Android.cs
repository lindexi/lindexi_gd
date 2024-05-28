using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
<<<<<<<< HEAD:UnoDemo/UnoFileDownloader/UnoFileDownloader.Mobile/Android/Main.Android.cs

using Com.Nostra13.Universalimageloader.Core;

using Microsoft.UI.Xaml.Media;

namespace UnoFileDownloader.Droid
========
using Com.Nostra13.Universalimageloader.Core;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkiaSharpTest.Droid
>>>>>>>> 6d7aae9cafeaffc475cdd5b151ec630f0d9f8ee8:JagobawearjiNeewhiqakerki/SkiaSharpTest/SkiaSharpTest.Mobile/Android/Main.Android.cs
{
    [global::Android.App.ApplicationAttribute(
        Label = "@string/ApplicationName",
        Icon = "@mipmap/icon",
        LargeHeap = true,
        HardwareAccelerated = true,
        Theme = "@style/AppTheme"
    )]
    public class Application : Microsoft.UI.Xaml.NativeApplication
    {
        public Application(IntPtr javaReference, JniHandleOwnership transfer)
<<<<<<<< HEAD:UnoDemo/UnoFileDownloader/UnoFileDownloader.Mobile/Android/Main.Android.cs
            : base(() => new AppHead(), javaReference, transfer)
========
            : base(() => new App(), javaReference, transfer)
>>>>>>>> 6d7aae9cafeaffc475cdd5b151ec630f0d9f8ee8:JagobawearjiNeewhiqakerki/SkiaSharpTest/SkiaSharpTest.Mobile/Android/Main.Android.cs
        {
            ConfigureUniversalImageLoader();
        }

        private static void ConfigureUniversalImageLoader()
        {
            // Create global configuration and initialize ImageLoader with this config
            ImageLoaderConfiguration config = new ImageLoaderConfiguration
                .Builder(Context)
                .Build();

            ImageLoader.Instance.Init(config);

            ImageSource.DefaultImageLoader = ImageLoader.Instance.LoadImageAsync;
        }
    }
}