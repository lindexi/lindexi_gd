using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace WindowFPS;

internal sealed class WindowCapture : IDisposable
{
    private const DirectXPixelFormat Format = DirectXPixelFormat.B8G8R8A8UIntNormalized;
    private const int BufferCount = 2;
    private readonly object _gate = new();
    private readonly Queue<TimeSpan> _frames = new();
    private readonly GraphicsCaptureItem _item;
    private readonly IDirect3DDevice _device;
    private readonly Direct3D11CaptureFramePool _pool;
    private readonly GraphicsCaptureSession _session;
    private SizeInt32 _size;
    private bool _disposed;
    private bool _stopped;

    internal WindowCapture(GraphicsCaptureItem item, IDirect3DDevice device)
    {
        _item = item;
        _device = device;
        _size = item.Size;
        _pool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, Format, BufferCount, _size);
        try
        {
            _session = _pool.CreateCaptureSession(item);
            _session.IsCursorCaptureEnabled = false;
            _pool.FrameArrived += OnFrameArrived;
            _item.Closed += OnItemClosed;
            _session.StartCapture();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal (bool Stopped, double? Fps) GetReading()
    {
        lock (_gate)
        {
            TrimFrames(Stopwatch.GetElapsedTime(0));
            if (_stopped || _frames.Count == 0)
            {
                return (_stopped, null);
            }

            return (false, _frames.Count);
        }
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        lock (_gate)
        {
            if (_stopped)
            {
                return;
            }

            try
            {
                SizeInt32 size;
                using (var frame = sender.TryGetNextFrame())
                {
                    if (frame is null)
                    {
                        return;
                    }

                    var timestamp = frame.SystemRelativeTime;
                    if (_frames.Count == 0 || timestamp > _frames.Last())
                    {
                        _frames.Enqueue(timestamp);
                        TrimFrames(timestamp);
                    }
                    size = frame.ContentSize;
                }

                if (size.Width > 0 && size.Height > 0 && (size.Width != _size.Width || size.Height != _size.Height))
                {
                    sender.Recreate(_device, Format, BufferCount, size);
                    _size = size;
                }
            }
            catch (COMException ex)
            {
                Trace.TraceError("Window capture failed: {0}", ex);
                _stopped = true;
            }
        }
    }

    private void OnItemClosed(GraphicsCaptureItem sender, object args)
    {
        lock (_gate)
        {
            _stopped = true;
        }
    }

    private void TrimFrames(TimeSpan now)
    {
        var cutoff = now - TimeSpan.FromSeconds(1);
        while (_frames.TryPeek(out var timestamp) && timestamp <= cutoff)
        {
            _frames.Dequeue();
        }
    }

    /// <summary>Stops frame delivery before releasing the capture resources.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stopped = true;
        }

        _item.Closed -= OnItemClosed;
        _pool.FrameArrived -= OnFrameArrived;
        try
        {
            _session?.Dispose();
        }
        finally
        {
            _pool.Dispose();
        }
    }
}
