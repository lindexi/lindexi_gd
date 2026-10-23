using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D9;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;

namespace MediaSamples;

internal sealed unsafe class SourceReader9Session : IDisposable
{
    private readonly VideoDevice _graphics;
    private IMFSourceReader* _reader;
    private IDirect3DDeviceManager9* _manager;
    private IDirect3DSurface9* _upload;
    private IMFSample* _pending;
    private readonly string _path;
    private bool _software;
    private bool _started;
    private uint _width, _height;
    private readonly Stopwatch _clock = new();
    private long? _firstTimestamp;
    private long _timestamp;
    internal object Gate { get; } = new();
    internal long Frames { get; private set; }

    internal SourceReader9Session(HWND window, string path)
    {
        _path = path;
        _graphics = new VideoDevice(window, true, false);
        try
        {
            PInvoke.MFStartup(PInvoke.MF_VERSION, 0).ThrowOnFailure();
            _started = true;
            uint token = 0;
            IDirect3DDeviceManager9* manager = null;
            PInvoke.DXVA2CreateDirect3DDeviceManager9(&token, &manager).ThrowOnFailure();
            _manager = manager;
            manager->ResetDevice((IDirect3DDevice9*)_graphics.Device9, token).ThrowOnFailure();
            InitializeReader();
            _clock.Start();
            Console.WriteLine($"Initialized SourceReader9; process={RuntimeInformation.ProcessArchitecture}; source={path}");
        }
        catch { Dispose(); throw; }
    }

    private void InitializeReader()
    {
        IMFAttributes* attributes = null;
        IMFMediaType* type = null;
        try
        {
            PInvoke.MFCreateAttributes(&attributes, 2).ThrowOnFailure();
            var key = PInvoke.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS;
            attributes->SetUINT32(&key, _software ? 0u : 1u).ThrowOnFailure();
            if (!_software)
            {
                key = PInvoke.MF_SOURCE_READER_D3D_MANAGER;
                attributes->SetUnknown(&key, (IUnknown*)_manager).ThrowOnFailure();
            }
            IMFSourceReader* reader = null;
            fixed (char* path = _path) PInvoke.MFCreateSourceReaderFromURL(path, attributes, &reader).ThrowOnFailure();
            _reader = reader;
            reader->SetStreamSelection(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_ALL_STREAMS), false).ThrowOnFailure();
            reader->SetStreamSelection(VideoStream, true).ThrowOnFailure();
            PInvoke.MFCreateMediaType(&type).ThrowOnFailure();
            key = PInvoke.MF_MT_MAJOR_TYPE;
            var value = PInvoke.MFMediaType_Video;
            type->SetGUID(&key, &value).ThrowOnFailure();
            key = PInvoke.MF_MT_SUBTYPE;
            value = PInvoke.MFVideoFormat_NV12;
            type->SetGUID(&key, &value).ThrowOnFailure();
            reader->SetCurrentMediaType(VideoStream, null, type).ThrowOnFailure();
        }
        finally { VideoDevice.Release(type); VideoDevice.Release(attributes); }
        UpdateSize();
        Console.WriteLine(_software ? "D3D9 software decode fallback; NV12 -> BGRA surface upload -> StretchRect -> Present." : "D3D9 DXVA2 device manager enabled; GPU decode preferred.");
    }

    private const uint VideoStream = unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM);

    private void UpdateSize()
    {
        IMFMediaType* type = null;
        _reader->GetCurrentMediaType(VideoStream, &type).ThrowOnFailure();
        try
        {
            var key = PInvoke.MF_MT_FRAME_SIZE;
            ulong size = 0;
            type->GetUINT64(&key, &size).ThrowOnFailure();
            _width = (uint)(size >> 32);
            _height = (uint)size;
            VideoDevice.Release(_upload); _upload = null;
        }
        finally { VideoDevice.Release(type); }
    }

    internal void Tick()
    {
        if (_pending == null)
        {
            uint actual = 0, flags = 0;
            long timestamp = 0;
            IMFSample* sample = null;
            var status = _reader->ReadSample(VideoStream, 0, &actual, &flags, &timestamp, &sample);
            if (status.Failed)
            {
                VideoDevice.Release(sample);
                // A virtual adapter can create D3D9 successfully without providing a DXVA decoder.
                if (!_software && Frames == 0 && status.Value == unchecked((int)0xC00D36B4))
                {
                    Console.WriteLine($"DXVA2 decoding unavailable: 0x{status.Value:X8}; recreating software reader.");
                    VideoDevice.Release(_reader); _reader = null;
                    _software = true;
                    InitializeReader();
                    _clock.Restart();
                    return;
                }
                status.ThrowOnFailure();
            }
            if ((flags & (uint)MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
            {
                VideoDevice.Release(sample);
                var position = new PROPVARIANT();
                position.Anonymous.Anonymous.vt = VARENUM.VT_I8;
                var format = Guid.Empty;
                _reader->SetCurrentPosition(&format, &position).ThrowOnFailure();
                _firstTimestamp = null;
                _clock.Restart();
                return;
            }
            if ((flags & (uint)MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) != 0) UpdateSize();
            _pending = sample;
            if (sample == null) return;
            _timestamp = timestamp;
            _firstTimestamp ??= timestamp;
        }
        if (_clock.Elapsed.TotalMilliseconds * 10000 < _timestamp - _firstTimestamp.GetValueOrDefault()) return;
        try { Render(_pending); }
        finally { VideoDevice.Release(_pending); _pending = null; }
        Frames++;
        if (Frames == 1) Console.WriteLine("First decoded D3D9 sample rendered with StretchRect and presented.");
    }

    private void Render(IMFSample* sample)
    {
        IMFMediaBuffer* buffer = null;
        IDirect3DSurface9* surface = null;
        IDirect3DSurface9* backBuffer = null;
        try
        {
            sample->GetBufferByIndex(0, &buffer).ThrowOnFailure();
            if (!_software)
            {
                var service = PInvoke.MR_BUFFER_SERVICE;
                var iid = typeof(IDirect3DSurface9).GUID;
                void* value = null;
                PInvoke.MFGetService((IUnknown*)buffer, &service, &iid, &value).ThrowOnFailure();
                surface = (IDirect3DSurface9*)value;
            }
            else
            {
                if (_upload == null)
                {
                    IDirect3DSurface9* upload = null;
                    _graphics.Device9->CreateOffscreenPlainSurface(_width, _height, D3DFORMAT.D3DFMT_A8R8G8B8,
                        D3DPOOL.D3DPOOL_DEFAULT, &upload, null).ThrowOnFailure();
                    _upload = upload;
                }
                byte* source = null;
                uint maximum = 0, length = 0;
                buffer->Lock(&source, &maximum, &length).ThrowOnFailure();
                try
                {
                    uint rowCount = _height + (_height + 1) / 2;
                    if (length < checked(_width * rowCount)) throw new InvalidDataException("Incomplete NV12 frame.");
                    var locked = new D3DLOCKED_RECT();
                    _upload->LockRect(&locked, null, 0).ThrowOnFailure();
                    try
                    {
                        // Virtual adapters may lack NV12 surfaces, so upload RGB while retaining D3D9 presentation.
                        byte* chroma = source + _width * _height;
                        for (uint row = 0; row < _height; row++)
                        {
                            byte* destination = (byte*)locked.pBits + row * locked.Pitch;
                            for (uint column = 0; column < _width; column++)
                            {
                                int y = Math.Max(0, source[row * _width + column] - 16);
                                uint uv = row / 2 * _width + (column & ~1u);
                                int u = chroma[uv] - 128, v = chroma[uv + 1] - 128;
                                destination[column * 4] = (byte)Math.Clamp((298 * y + 516 * u + 128) >> 8, 0, 255);
                                destination[column * 4 + 1] = (byte)Math.Clamp((298 * y - 100 * u - 208 * v + 128) >> 8, 0, 255);
                                destination[column * 4 + 2] = (byte)Math.Clamp((298 * y + 409 * v + 128) >> 8, 0, 255);
                                destination[column * 4 + 3] = 255;
                            }
                        }
                    }
                    finally { _upload->UnlockRect().ThrowOnFailure(); }
                }
                finally { buffer->Unlock().ThrowOnFailure(); }
            }
            _graphics.Device9->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE.D3DBACKBUFFER_TYPE_MONO, &backBuffer).ThrowOnFailure();
            _graphics.Device9->StretchRect(_software ? _upload : surface, null, backBuffer, null, D3DTEXTUREFILTERTYPE.D3DTEXF_LINEAR).ThrowOnFailure();
            _graphics.Device9->Present(null, null, default, null).ThrowOnFailure();
        }
        finally { VideoDevice.Release(backBuffer); VideoDevice.Release(surface); VideoDevice.Release(buffer); }
    }

    /// <summary>Releases decoder and D3D9 resources before shutting down Media Foundation.</summary>
    public void Dispose()
    {
        VideoDevice.Release(_pending); _pending = null;
        VideoDevice.Release(_reader); _reader = null;
        VideoDevice.Release(_upload); _upload = null;
        VideoDevice.Release(_manager); _manager = null;
        _graphics.Dispose();
        if (_started) { PInvoke.MFShutdown().ThrowOnFailure(); _started = false; }
    }
}
