using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LajeweekallqeFiwigewee
{
    /// <summary>
    ///     可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Slider.AddHandler(PointerPressedEvent, new PointerEventHandler(Slider_OnPointerPressed), true);
            Slider.AddHandler(PointerReleasedEvent, new PointerEventHandler(Slider_OnPointerReleased), true);
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(MainPage), new PropertyMetadata(default(double), (s
                , e) =>
            {
                ((MainPage) s)._lastValue = (double) e.OldValue;
            }));

        public double Value
        {
            get => (double) GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private double _lastValue;

        private Storyboard _storyboard;

        private void Slider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var slider = (Slider) sender;

            ClickPoint = e.GetCurrentPoint(slider).Position;
        }

        private Point ClickPoint { set; get; }

        private void Slider_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var slider = (Slider) sender;

            var point = e.GetCurrentPoint(slider).Position;

            var x = point.X - ClickPoint.X;
            var y = point.Y - ClickPoint.Y;
            var length = x * x + y * y;
            if (length < 10)
            {
                AnimationValue();
            }
        }

        private void AnimationValue()
        {
            var storyboard = new Storyboard();

            var animation = new DoubleAnimation
            {
                From = _lastValue,
                To = Value,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = new CubicEase(),
                EnableDependentAnimation = true
            };

            Storyboard.SetTarget(animation, Slider);
            Storyboard.SetTargetProperty(animation, "Value");

            storyboard.BeginTime = TimeSpan.Zero;
            storyboard.Children.Add(animation);

            storyboard.Begin();

            _storyboard = storyboard;
        }
    }
}