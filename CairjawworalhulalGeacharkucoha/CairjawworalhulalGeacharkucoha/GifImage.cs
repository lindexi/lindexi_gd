using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace CairjawworalhulalGeacharkucoha;

class GifImage : Image
{
    private bool _isInitialized;
    private GifBitmapDecoder _gifDecoder;
    private Int32AnimationUsingKeyFrames _animation;

    public int FrameIndex
    {
        get { return (int) GetValue(FrameIndexProperty); }
        set { SetValue(FrameIndexProperty, value); }
    }

    private void Initialize()
    {
        _gifDecoder = new GifBitmapDecoder(GifSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

        this.Source = _gifDecoder.Frames[0];

        var keyFrames = new Int32KeyFrameCollection();
        TimeSpan last = TimeSpan.Zero;
        for (int i = 0; i < _gifDecoder.Frames.Count; i++)
        {
            var gifDecoderFrame = _gifDecoder.Frames[i];
            var bitmapMetadata = gifDecoderFrame.Metadata as BitmapMetadata;
            var delayTime = bitmapMetadata?.GetQuery("/grctlext/Delay") as ushort?;
            var delay = delayTime ?? 10;
            last += TimeSpan.FromMilliseconds(delay * 10);
            keyFrames.Add(new DiscreteInt32KeyFrame(i, KeyTime.FromTimeSpan(last)));
        }

        _animation = new Int32AnimationUsingKeyFrames()
        {
            KeyFrames = keyFrames,
            RepeatBehavior = RepeatBehavior.Forever,
        };

        _isInitialized = true;
    }

    static GifImage()
    {
        VisibilityProperty.OverrideMetadata(typeof(GifImage),
            new FrameworkPropertyMetadata(VisibilityPropertyChanged));
    }

    private static void VisibilityPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if ((Visibility) e.NewValue == Visibility.Visible)
        {
            ((GifImage) sender).StartAnimation();
        }
        else
        {
            ((GifImage) sender).StopAnimation();
        }
    }

    public static readonly DependencyProperty FrameIndexProperty =
        DependencyProperty.Register("FrameIndex", typeof(int), typeof(GifImage), new UIPropertyMetadata(0, new PropertyChangedCallback(ChangingFrameIndex)));

    static void ChangingFrameIndex(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
    {
        var gifImage = obj as GifImage;
        gifImage.Source = gifImage._gifDecoder.Frames[(int) ev.NewValue];
    }

    /// <summary>
    /// Defines whether the animation starts on it's own
    /// </summary>
    public bool AutoStart
    {
        get { return (bool) GetValue(AutoStartProperty); }
        set { SetValue(AutoStartProperty, value); }
    }

    public static readonly DependencyProperty AutoStartProperty =
        DependencyProperty.Register("AutoStart", typeof(bool), typeof(GifImage), new UIPropertyMetadata(false, AutoStartPropertyChanged));

    private static void AutoStartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool) e.NewValue)
            (sender as GifImage).StartAnimation();
    }

    public static readonly DependencyProperty GifSourceProperty = DependencyProperty.Register(
        "GifSource", typeof(Uri), typeof(GifImage), new UIPropertyMetadata(default(Uri), GifSourcePropertyChanged));

    public Uri GifSource
    {
        get { return (Uri) GetValue(GifSourceProperty); }
        set { SetValue(GifSourceProperty, value); }
    }

    private static void GifSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        (sender as GifImage).Initialize();
    }

    /// <summary>
    /// Starts the animation
    /// </summary>
    public void StartAnimation()
    {
        if (!_isInitialized)
            this.Initialize();

        BeginAnimation(FrameIndexProperty, _animation);
    }

    /// <summary>
    /// Stops the animation
    /// </summary>
    public void StopAnimation()
    {
        BeginAnimation(FrameIndexProperty, null);
    }
}