using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JacaikecejihaiweHeqajearhi;

internal enum TransparencyDemoState
{
    OpaqueX8,
    Upgrading,
    AlphaModeApplied,
    Faulted,
}

internal sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private TransparencyDemoState _transparencyState = TransparencyDemoState.OpaqueX8;
    private double _framesPerSecond;
    private double _averageFrameTimeMilliseconds;
    private long _renderedFrameCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TransparencyDemoState TransparencyState
    {
        get => _transparencyState;
        private set
        {
            if (SetField(ref _transparencyState, value))
            {
                OnPropertyChanged(nameof(IsUpgradeEnabled));
            }
        }
    }

    public bool IsUpgradeEnabled => TransparencyState == TransparencyDemoState.OpaqueX8;

    public double FramesPerSecond
    {
        get => _framesPerSecond;
        private set => SetField(ref _framesPerSecond, value);
    }

    public double AverageFrameTimeMilliseconds
    {
        get => _averageFrameTimeMilliseconds;
        private set => SetField(ref _averageFrameTimeMilliseconds, value);
    }

    public long RenderedFrameCount
    {
        get => _renderedFrameCount;
        private set => SetField(ref _renderedFrameCount, value);
    }

    public void MarkUpgrading()
    {
        TransparencyState = TransparencyDemoState.Upgrading;
    }

    public void MarkAlphaModeApplied()
    {
        TransparencyState = TransparencyDemoState.AlphaModeApplied;
    }

    public void MarkFaulted()
    {
        TransparencyState = TransparencyDemoState.Faulted;
    }

    public void UpdateRenderingMetrics(
        double framesPerSecond,
        double averageFrameTimeMilliseconds,
        long renderedFrameCount)
    {
        FramesPerSecond = framesPerSecond;
        AverageFrameTimeMilliseconds = averageFrameTimeMilliseconds;
        RenderedFrameCount = renderedFrameCount;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
