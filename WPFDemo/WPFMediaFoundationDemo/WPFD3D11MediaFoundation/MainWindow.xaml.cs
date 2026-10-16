using System.Windows;
using MediaSamples;

namespace WPFD3D11MediaFoundation;

/// <summary>
/// Displays Source Reader NV12 samples processed by D3D11 through a shared D3D9 WPF surface.
/// </summary>
public partial class MainWindow : Window
{
    private readonly WpfPresenter _presenter;

    /// <summary>Creates the hardware-decoded video presenter.</summary>
    public MainWindow()
    {
        InitializeComponent();
        _presenter = new WpfPresenter(this, Image, SampleKind.SourceReader11);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => _presenter.Initialize();
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) { }
}
