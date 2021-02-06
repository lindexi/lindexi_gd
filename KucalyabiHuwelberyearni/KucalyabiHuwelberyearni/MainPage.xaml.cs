using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace KucalyabiHuwelberyearni
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayRequest displayRequest = new DisplayRequest();
            try
            {

                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                _mediaCapture.VideoDeviceController.DesiredOptimization = MediaCaptureOptimization.Quality;
                _mediaCapture.VideoDeviceController.PrimaryUse = CaptureUse.Video;
                _mediaCapture.VideoDeviceController.TrySetPowerlineFrequency(PowerlineFrequency.SixtyHertz);

                try
                {
                    var comboBox = ComboBox;

                    var availableMediaStreamProperties = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord).ToList().OfType<VideoEncodingProperties>()
                        //.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);
                        .ToList() ;

                    // Populate the combo box with the entries
                    foreach (VideoEncodingProperties property in availableMediaStreamProperties)
                    {
                        ComboBoxItem comboBoxItem = new ComboBoxItem();
                        comboBoxItem.Content = property.Width + "x" + property.Height + " " + property.FrameRate + "FPS " + property.Subtype;
                        comboBoxItem.Tag = property;
                        comboBox.Items.Add(comboBoxItem);
                    }
                }
                catch (Exception)
                {

                }

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                return;
            }

            try
            {
                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
            }
            catch (System.IO.FileLoadException)
            {

            }

        }

        MediaCapture _mediaCapture;

        private async void ComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
                var encodingProperties = (selectedItem.Tag as VideoEncodingProperties);
                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, encodingProperties);
            }
            catch (Exception)
            {
              
            }
        }
    }
}
