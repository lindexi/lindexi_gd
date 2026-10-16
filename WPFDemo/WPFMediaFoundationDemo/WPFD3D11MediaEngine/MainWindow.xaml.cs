using System.Windows;
using System.Windows.Media;
using MediaSamples;
using Microsoft.Win32;

namespace WPFD3D11MediaEngine;

/// <summary>Hosts Media Engine frame-server output in a WPF D3DImage.</summary>
public partial class MainWindow : Window
{
    private readonly WpfPresenter _presenter;
    private bool _updatingTime;

    /// <summary>Creates a player with open, play, pause and seek controls.</summary>
    public MainWindow()
    {
        InitializeComponent();
        _presenter = new WpfPresenter(this, Image, SampleKind.MediaEngine);
        CompositionTarget.Rendering += UpdateProgress;
        Closed += (_, _) => CompositionTarget.Rendering -= UpdateProgress;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => _presenter.Initialize();
    private void ImageBorder_SizeChanged(object sender, SizeChangedEventArgs e) { }
    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();
        if (dialog.ShowDialog(this) == true) _presenter.Session?.Open(dialog.FileName);
    }
    private void PlayButton_Click(object sender, RoutedEventArgs e) => _presenter.Session?.Play();
    private void PauseButton_Click(object sender, RoutedEventArgs e) => _presenter.Session?.Pause();
    private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_updatingTime) _presenter?.Session?.Seek(e.NewValue);
    }
    private void UpdateProgress(object? sender, EventArgs e)
    {
        var session = _presenter.Session;
        if (session == null) return;
        double duration = session.Duration;
        if (!double.IsFinite(duration) || duration <= 0) return;
        _updatingTime = true;
        try
        {
            ProgressSlider.Maximum = duration;
            ProgressSlider.Value = session.CurrentTime;
            CurrentTextBlock.Text = TimeSpan.FromSeconds(session.CurrentTime).ToString(@"hh\:mm\:ss");
            DurationTextBlock.Text = TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss");
        }
        finally { _updatingTime = false; }
    }
}
