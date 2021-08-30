using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
using Microsoft.Graphics.Canvas.UI.Xaml;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace KeanearkallhawDaherenenallyi
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly InkSynchronizer _inkSynchronizer;

        public MainPage()
        {
            this.InitializeComponent();
            _inkSynchronizer = InkCanvas.InkPresenter.ActivateCustomDrying();
            InkCanvas.InkPresenter.SetPredefinedConfiguration(InkPresenterPredefinedConfiguration
                .SimpleMultiplePointer);
            InkCanvas.InkPresenter.InputDeviceTypes =
                CoreInputDeviceTypes.Touch | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
            InkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            InkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
            InkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction =
                InkInputRightDragAction.LeaveUnprocessed;
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            var id = args.CurrentPoint.PointerId;
            // 需要根据 id 分开多个手指

            InkStrokeBuilder.SetDefaultDrawingAttributes(new InkDrawingAttributes()
            {
                Color = Colors.Blue,
                Size = new Size(5, 5)
            });

            _currentPointerList.AddRange(args.GetIntermediatePoints());
            _inkStroke = InkStrokeBuilder.CreateStrokeFromInkPoints(
                _currentPointerList.Select(t => new InkPoint(t.Position, t.Properties.Pressure)), Matrix3x2.Identity);

            Canvas.Invalidate();
        }

        private readonly List<PointerPoint> _currentPointerList = new List<PointerPoint>();
        private InkStroke _inkStroke;

        private InkStrokeBuilder InkStrokeBuilder { get; } = new InkStrokeBuilder();

        private void Canvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_inkStroke != null)
            {
                args.DrawingSession.DrawInk(new []{_inkStroke});
            }
        }
    }
}
