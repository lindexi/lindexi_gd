using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace KalljelcufedilaiCaiheebeferwhearlair
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            var inkPresenter = InkCanvas.InkPresenter;
            inkPresenter.InputDeviceTypes =
                CoreInputDeviceTypes.Touch | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen;
            var defaultDrawingAttributes = inkPresenter.CopyDefaultDrawingAttributes();
            defaultDrawingAttributes.Color = Colors.Red;
            defaultDrawingAttributes.ModelerAttributes.UseVelocityBasedPressure = true;
            defaultDrawingAttributes.ModelerAttributes.PredictionTime = TimeSpan.FromMilliseconds(20);
            defaultDrawingAttributes.FitToCurve = true;
            defaultDrawingAttributes.Size = new Size(5, 5);
            inkPresenter.UpdateDefaultDrawingAttributes(defaultDrawingAttributes);

            inkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
        }

        private void UnprocessedInput_PointerMoved(Windows.UI.Input.Inking.InkUnprocessedInput sender, PointerEventArgs args)
        {
        }
    }
}
