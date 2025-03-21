using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LibunallluWhaganowar
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();


            InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            InkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            InkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            InkCanvas.InkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;

            InkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;

            //InkCanvas.InkPresenter.SetPredefinedConfiguration(InkPresenterPredefinedConfiguration.SimpleMultiplePointer);
            var inkStrokeBuilder = new InkStrokeBuilder();
            _inkStrokeBuilder = inkStrokeBuilder;
        }

        private List<PointerPoint> _list = new List<PointerPoint>();

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            //sender.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            InkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            var inkPresenterIsInputEnabled = InkCanvas.InkPresenter.IsInputEnabled;
            //InkCanvas.InkPresenter.StrokeContainer.AddStroke(new InkStroke());

            //_inkStrokeBuilder.BeginStroke(args.CurrentPoint);
            _list.Add(args.CurrentPoint);
        }

        private void UnprocessedInput_PointerMoved(Windows.UI.Input.Inking.InkUnprocessedInput sender, PointerEventArgs args)
        {
            //_inkStrokeBuilder.AppendToStroke(args.CurrentPoint);
            _list.Add(args.CurrentPoint);

            if (_inkStroke != null)
            {
                InkCanvas.InkPresenter.StrokeContainer.Clear();
            }

            InkStroke inkStroke = _inkStrokeBuilder.CreateStroke(_list.Select(t => t.Position));
            InkCanvas.InkPresenter.StrokeContainer.AddStroke(inkStroke);
            _inkStroke = inkStroke;
        }

        private InkStroke _inkStroke;

        private void StrokeInput_StrokeContinued(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {

        }

        private int _count;
        private InkStrokeBuilder _inkStrokeBuilder;
    }
}
