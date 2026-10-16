using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D9;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows8.0")]

namespace MediaSamples;

internal enum SampleKind { SourceReader11, SourceReader9, MediaEngine, CaptureEngine }

internal unsafe sealed class MediaSession : IDisposable
{
    internal VideoDevice Graphics { get; }
    internal long Frames { get; private set; }
    internal readonly object Gate = new();
    private IMFSourceReader* _reader;
    private IMFDXGIDeviceManager* _manager;
    private IDirect3DDeviceManager9* _manager9;
    private IMFMediaEngine* _engine;
    private IMFCaptureEngine* _capture;
    private IMFCapturePreviewSink* _preview;
    private NativeCallback? _events;
    private NativeCallback? _samples;
    private readonly SampleKind _kind;
    private readonly Stopwatch _clock = new();
    private IMFSample* _pending;
    private long _timestamp;
    private long? _firstTimestamp;
    private int _captureReady;
    private bool _previewStarted;
    private bool _disposed;
    private bool _mfStarted;
    private ID3D11Texture2D* _upload;
    private uint _captureWidth, _captureHeight;
    private byte[]? _capturePixels;

    internal MediaSession(HWND window, SampleKind kind, string path, bool shared = false)
    {
        _kind = kind;
        Graphics = new VideoDevice(window, kind == SampleKind.SourceReader9, shared);
        try
        {
            PInvoke.MFStartup(PInvoke.MF_VERSION, 0).ThrowOnFailure();
            _mfStarted = true;
            uint token = 0;
            if (kind == SampleKind.SourceReader9)
            {
                IDirect3DDeviceManager9* manager = null;
                PInvoke.DXVA2CreateDirect3DDeviceManager9(&token, &manager).ThrowOnFailure();
                _manager9 = manager;
                manager->ResetDevice((IDirect3DDevice9*)Graphics.Device9, token).ThrowOnFailure();
            }
            else
            {
                IMFDXGIDeviceManager* manager = null;
                PInvoke.MFCreateDXGIDeviceManager(&token, &manager).ThrowOnFailure();
                _manager = manager;
                manager->ResetDevice((IUnknown*)Graphics.Device, token).ThrowOnFailure();
            }
            if (kind == SampleKind.MediaEngine) InitializeEngine(path);
            else if (kind == SampleKind.CaptureEngine) InitializeCapture();
            else InitializeReader(path);
            _clock.Start();
            Console.WriteLine($"Initialized {kind}; process={RuntimeInformation.ProcessArchitecture}; source={path}");
        }
        catch { Dispose(); throw; }
    }

    private static void Set(IMFAttributes* attributes, Guid key, uint value) => attributes->SetUINT32(&key, value).ThrowOnFailure();
    private static void Set(IMFAttributes* attributes, Guid key, Guid value) => attributes->SetGUID(&key, &value).ThrowOnFailure();
    private static void Set(IMFAttributes* attributes, Guid key, void* value) => attributes->SetUnknown(&key, (IUnknown*)value).ThrowOnFailure();
    private static IMFAttributes* Attributes(uint capacity)
    {
        IMFAttributes* attributes = null;
        PInvoke.MFCreateAttributes(&attributes, capacity).ThrowOnFailure();
        return attributes;
    }

    private void InitializeReader(string path)
    {
        var attributes = Attributes(2);
        try
        {
            Set(attributes, PInvoke.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1u);
            Set(attributes, PInvoke.MF_SOURCE_READER_D3D_MANAGER, _manager != null ? _manager : (void*)_manager9);
            IMFSourceReader* reader = null;
            fixed (char* url = path) PInvoke.MFCreateSourceReaderFromURL(url, attributes, &reader).ThrowOnFailure();
            _reader = reader;
        }
        finally { VideoDevice.Release(attributes); }
        _reader->SetStreamSelection(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_ALL_STREAMS), false).ThrowOnFailure();
        _reader->SetStreamSelection(VideoStream, true).ThrowOnFailure();
        IMFMediaType* type = null;
        PInvoke.MFCreateMediaType(&type).ThrowOnFailure();
        try
        {
            Set((IMFAttributes*)type, PInvoke.MF_MT_MAJOR_TYPE, PInvoke.MFMediaType_Video);
            Set((IMFAttributes*)type, PInvoke.MF_MT_SUBTYPE, PInvoke.MFVideoFormat_NV12);
            _reader->SetCurrentMediaType(VideoStream, null, type).ThrowOnFailure();
        }
        finally { VideoDevice.Release(type); }
        UpdateReaderSize();
        Console.WriteLine("Source Reader hardware transforms enabled; NV12 output requested; GPU sample required.");
    }

    private void UpdateReaderSize()
    {
        IMFMediaType* type = null;
        _reader->GetCurrentMediaType(VideoStream, &type).ThrowOnFailure();
        try
        {
            ulong size = 0;
            var key = PInvoke.MF_MT_FRAME_SIZE;
            type->GetUINT64(&key, &size).ThrowOnFailure();
            uint width = (uint)(size >> 32), height = (uint)size;
            MFVideoArea area = default;
            uint length = 0;
            key = PInvoke.MF_MT_MINIMUM_DISPLAY_APERTURE;
            var aperture = type->GetBlob(&key, (byte*)&area, (uint)sizeof(MFVideoArea), &length);
            if (aperture.Succeeded && length == sizeof(MFVideoArea) && area.Area.cx > 0 && area.Area.cy > 0)
            {
                width = (uint)area.Area.cx;
                height = (uint)area.Area.cy;
            }
            Graphics.VideoWidth = width;
            Graphics.VideoHeight = height;
            Console.WriteLine($"Visible video size: {Graphics.VideoWidth}x{Graphics.VideoHeight}.");
        }
        finally { VideoDevice.Release(type); }
    }

    private const uint VideoStream = unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM);

    private void InitializeEngine(string path)
    {
        _events = new NativeCallback(typeof(IMFMediaEngineNotify).GUID, true);
        _events.Notify = (kind, first, second) =>
        {
            Console.WriteLine($"MediaEngine event={kind}, parameter=0x{second:X8}");
            if (kind == (uint)MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ERROR)
                _events.Failure = new COMException($"MediaEngine error {first}, HRESULT=0x{second:X8}", unchecked((int)second));
        };
        var attributes = Attributes(3);
        IMFMediaEngineClassFactory* factory = null;
        try
        {
            Set(attributes, PInvoke.MF_MEDIA_ENGINE_CALLBACK, _events.Pointer);
            Set(attributes, PInvoke.MF_MEDIA_ENGINE_DXGI_MANAGER, _manager);
            Set(attributes, PInvoke.MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT, (uint)DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM);
            var clsid = new Guid("B44392DA-499B-446B-A4CB-005FEAD0E6D5");
            var iid = typeof(IMFMediaEngineClassFactory).GUID;
            void* value = null;
            PInvoke.CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, &value).ThrowOnFailure();
            factory = (IMFMediaEngineClassFactory*)value;
            IMFMediaEngine* engine = null;
            factory->CreateInstance(0, attributes, &engine).ThrowOnFailure();
            _engine = engine;
            Open(path);
            engine->SetLoop(true).ThrowOnFailure();
        }
        finally { VideoDevice.Release(factory); VideoDevice.Release(attributes); }
    }

    internal void Open(string path)
    {
        if (_engine == null) return;
        var value = Marshal.StringToBSTR(path);
        try { _engine->SetSource(new BSTR((char*)value)).ThrowOnFailure(); }
        finally { Marshal.FreeBSTR(value); }
        _engine->Play().ThrowOnFailure();
    }

    internal void Play() { if (_engine != null) _engine->Play().ThrowOnFailure(); }
    internal void Pause() { if (_engine != null) _engine->Pause().ThrowOnFailure(); }
    internal void Seek(double time) { if (_engine != null) _engine->SetCurrentTime(time).ThrowOnFailure(); }
    internal double Duration => _engine == null ? 0 : _engine->GetDuration();
    internal double CurrentTime => _engine == null ? 0 : _engine->GetCurrentTime();

    internal static uint CountCaptureDevices()
    {
        PInvoke.MFStartup(PInvoke.MF_VERSION, 0).ThrowOnFailure();
        IMFAttributes* attributes = null;
        IMFActivate** devices = null;
        uint count = 0;
        try
        {
            attributes = Attributes(1);
            Set(attributes, PInvoke.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, PInvoke.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
            PInvoke.MFEnumDeviceSources(attributes, &devices, &count).ThrowOnFailure();
            return count;
        }
        finally
        {
            if (devices != null)
            {
                for (uint index = 0; index < count; index++) VideoDevice.Release(devices[index]);
                PInvoke.CoTaskMemFree(devices);
            }
            VideoDevice.Release(attributes);
            PInvoke.MFShutdown().ThrowOnFailure();
        }
    }

    internal void CheckCaptureInitialization()
    {
        if (_events?.Failure is Exception failure)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal bool CaptureInitialized => Volatile.Read(ref _captureReady) == 1;

    private void InitializeCapture()
    {
        _events = new NativeCallback(typeof(IMFCaptureEngineOnEventCallback).GUID);
        _events.Invoke = value =>
        {
            var mediaEvent = (IMFMediaEvent*)value;
            Guid extended = default;
            HRESULT status = default;
            mediaEvent->GetStatus(&status).ThrowOnFailure();
            status.ThrowOnFailure();
            mediaEvent->GetExtendedType(&extended).ThrowOnFailure();
            Console.WriteLine($"Capture event={extended}, status={status}");
            if (extended == PInvoke.MF_CAPTURE_ENGINE_INITIALIZED) Interlocked.Exchange(ref _captureReady, 1);
        };
        var attributes = Attributes(3);
        IMFCaptureEngineClassFactory* factory = null;
        try
        {
            Set(attributes, PInvoke.MF_CAPTURE_ENGINE_D3D_MANAGER, _manager);
            Set(attributes, PInvoke.MF_CAPTURE_ENGINE_USE_VIDEO_DEVICE_ONLY, 1u);
            Set(attributes, PInvoke.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1u);
            var clsid = PInvoke.CLSID_MFCaptureEngineClassFactory;
            var iid = typeof(IMFCaptureEngineClassFactory).GUID;
            void* value = null;
            PInvoke.CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, &value).ThrowOnFailure();
            factory = (IMFCaptureEngineClassFactory*)value;
            clsid = PInvoke.CLSID_MFCaptureEngine;
            iid = typeof(IMFCaptureEngine).GUID;
            void* engine = null;
            factory->CreateInstance(&clsid, &iid, &engine).ThrowOnFailure();
            _capture = (IMFCaptureEngine*)engine;
            _capture->Initialize((IMFCaptureEngineOnEventCallback*)_events.Pointer, attributes, null, null).ThrowOnFailure();
        }
        finally { VideoDevice.Release(factory); VideoDevice.Release(attributes); }
    }

    private void StartPreview()
    {
        IMFCaptureSink* sink = null;
        IMFCaptureSource* source = null;
        IMFMediaType* type = null;
        try
        {
            _capture->GetSink(MF_CAPTURE_ENGINE_SINK_TYPE.MF_CAPTURE_ENGINE_SINK_TYPE_PREVIEW, &sink).ThrowOnFailure();
            _preview = (IMFCapturePreviewSink*)VideoDevice.Query(sink, typeof(IMFCapturePreviewSink).GUID);
            _capture->GetSource(&source).ThrowOnFailure();
            uint index = 0;
            source->GetStreamIndexFromFriendlyName(0xFFFFFFFCu, &index).ThrowOnFailure();
            source->GetCurrentDeviceMediaType(index, &type).ThrowOnFailure();
            ulong size = 0;
            var sizeKey = PInvoke.MF_MT_FRAME_SIZE;
            type->GetUINT64(&sizeKey, &size).ThrowOnFailure();
            _captureWidth = (uint)(size >> 32);
            _captureHeight = (uint)size;
            if (_captureWidth == 0 || _captureHeight == 0 || (_captureWidth & 1) != 0 || (_captureHeight & 1) != 0)
                throw new InvalidOperationException("Capture NV12 dimensions must be positive and even.");
            Graphics.VideoWidth = _captureWidth;
            Graphics.VideoHeight = _captureHeight;
            // The device's native format may be compressed; request a known preview layout.
            Set((IMFAttributes*)type, PInvoke.MF_MT_SUBTYPE, PInvoke.MFVideoFormat_NV12);
            Console.WriteLine($"Capture NV12 preview size: {_captureWidth}x{_captureHeight}.");
            uint sinkIndex = 0;
            _preview->AddStream(index, type, null, &sinkIndex).ThrowOnFailure();
            _samples = new NativeCallback(typeof(IMFCaptureEngineOnSampleCallback).GUID);
            _samples.Invoke = value =>
            {
                lock (Gate)
                {
                    if (_disposed || value == 0) return;
                    RenderSample((IMFSample*)value);
                    Graphics.Present();
                    Frames++;
                }
            };
            _preview->SetSampleCallback(sinkIndex, (IMFCaptureEngineOnSampleCallback*)_samples.Pointer).ThrowOnFailure();
            _capture->StartPreview().ThrowOnFailure();
            _previewStarted = true;
            Console.WriteLine("Capture preview started; waiting for actual samples.");
        }
        finally { VideoDevice.Release(type); VideoDevice.Release(source); VideoDevice.Release(sink); }
    }

    internal bool Tick()
    {
        if (_events?.Failure is Exception eventFailure) throw new InvalidOperationException("Native event callback failed.", eventFailure);
        if (_samples?.Failure is Exception sampleFailure) throw new InvalidOperationException("Native sample callback failed.", sampleFailure);
        if (_kind == SampleKind.CaptureEngine)
        {
            if (!_previewStarted && Volatile.Read(ref _captureReady) == 1) StartPreview();
            return false;
        }
        if (_engine != null)
        {
            long time = 0;
            var result = _engine->OnVideoStreamTick(&time);
            result.ThrowOnFailure();
            if (result.Value != 0) return false;
            Graphics.ResizeOutput();
            var rect = new RECT(0, 0, Graphics.Width, Graphics.Height);
            _engine->TransferVideoFrame((IUnknown*)Graphics.Target, null, &rect, null).ThrowOnFailure();
            Graphics.Present();
            Frames++;
            return true;
        }
        if (_pending == null)
        {
            uint actual = 0, flags = 0;
            long timestamp = 0;
            IMFSample* sample = null;
            _reader->ReadSample(VideoStream, 0, &actual, &flags, &timestamp, &sample).ThrowOnFailure();
            if ((flags & (uint)MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
            {
                VideoDevice.Release(sample);
                var position = new PROPVARIANT();
                position.Anonymous.Anonymous.vt = VARENUM.VT_I8;
                var format = Guid.Empty;
                _reader->SetCurrentPosition(&format, &position).ThrowOnFailure();
                _firstTimestamp = null;
                _clock.Restart();
                return false;
            }
            if ((flags & (uint)MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) != 0) UpdateReaderSize();
            _pending = sample;
            _timestamp = timestamp;
            if (sample == null) return false;
            _firstTimestamp ??= timestamp;
        }
        if (_clock.Elapsed.TotalMilliseconds * 10000 < _timestamp - _firstTimestamp.GetValueOrDefault()) return false;
        try { RenderSample(_pending); }
        finally { VideoDevice.Release(_pending); _pending = null; }
        if (_kind != SampleKind.SourceReader9) Graphics.Present();
        Frames++;
        if (Frames == 1) Console.WriteLine("First decoded GPU sample rendered and presented.");
        return true;
    }

    private void RenderSample(IMFSample* sample)
    {
        IMFMediaBuffer* buffer = null;
        sample->GetBufferByIndex(0, &buffer).ThrowOnFailure();
        try
        {
            if (_kind == SampleKind.SourceReader9)
            {
                var service = PInvoke.MR_BUFFER_SERVICE;
                var iid = typeof(IDirect3DSurface9).GUID;
                void* value = null;
                PInvoke.MFGetService((IUnknown*)buffer, &service, &iid, &value).ThrowOnFailure();
                var surface = (IDirect3DSurface9*)value;
                IDirect3DSurface9* backBuffer = null;
                try
                {
                    Graphics.Device9->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE.D3DBACKBUFFER_TYPE_MONO, &backBuffer).ThrowOnFailure();
                    Graphics.Device9->StretchRect(surface, null, backBuffer, null, D3DTEXTUREFILTERTYPE.D3DTEXF_LINEAR).ThrowOnFailure();
                    Graphics.Device9->Present(null, null, default, null).ThrowOnFailure();
                }
                finally { VideoDevice.Release(backBuffer); VideoDevice.Release(surface); }
                return;
            }
            var dxgiIid = typeof(IMFDXGIBuffer).GUID;
            void* dxgiValue = null;
            var query = ((IUnknown*)buffer)->QueryInterface(&dxgiIid, &dxgiValue);
            if (query.Succeeded)
            {
                var dxgi = (IMFDXGIBuffer*)dxgiValue;
                ID3D11Texture2D* texture = null;
                try
                {
                    var iid = typeof(ID3D11Texture2D).GUID;
                    void* value = null;
                    dxgi->GetResource(&iid, &value).ThrowOnFailure();
                    texture = (ID3D11Texture2D*)value;
                    uint slice = 0;
                    dxgi->GetSubresourceIndex(&slice).ThrowOnFailure();
                    Graphics.Process(texture, slice);
                }
                finally { VideoDevice.Release(texture); VideoDevice.Release(dxgi); }
            }
            else if (_kind == SampleKind.CaptureEngine && query.Value == unchecked((int)0x80004002))
            {
                UploadCaptureSample(buffer);
            }
            else
            {
                query.ThrowOnFailure();
            }
        }
        finally { VideoDevice.Release(buffer); }
    }

    private void UploadCaptureSample(IMFMediaBuffer* buffer)
    {
        int width = checked((int)_captureWidth), height = checked((int)_captureHeight);
        int byteCount = checked(width * height * 3 / 2);
        _capturePixels ??= new byte[byteCount];
        var iid = typeof(IMF2DBuffer2).GUID;
        void* value = null;
        var query = ((IUnknown*)buffer)->QueryInterface(&iid, &value);
        if (query.Succeeded)
        {
            var buffer2D = (IMF2DBuffer2*)value;
            try
            {
                fixed (byte* pixels = _capturePixels)
                    buffer2D->ContiguousCopyTo(pixels, (uint)byteCount).ThrowOnFailure();
            }
            finally { VideoDevice.Release(buffer2D); }
        }
        else
        {
            if (query.Value != unchecked((int)0x80004002)) query.ThrowOnFailure();
            byte* data = null;
            uint maximum = 0, length = 0;
            buffer->Lock(&data, &maximum, &length).ThrowOnFailure();
            try
            {
                if (length != byteCount) throw new InvalidOperationException("Unexpected contiguous NV12 capture buffer size.");
                Marshal.Copy((nint)data, _capturePixels, 0, byteCount);
            }
            finally { buffer->Unlock().ThrowOnFailure(); }
        }
        if (_upload == null)
        {
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = _captureWidth, Height = _captureHeight, MipLevels = 1, ArraySize = 1,
                Format = DXGI_FORMAT.DXGI_FORMAT_NV12,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE
            };
            ID3D11Texture2D* texture = null;
            Graphics.Device->CreateTexture2D(&desc, null, &texture).ThrowOnFailure();
            _upload = texture;
            Console.WriteLine($"Capture CPU NV12 buffer uploaded to GPU: {width}x{height}.");
        }
        fixed (byte* pixels = _capturePixels)
            Graphics.Context->UpdateSubresource((ID3D11Resource*)_upload, 0, (D3D11_BOX*)null, pixels, (uint)width, (uint)byteCount);
        Graphics.Process(_upload, 0);
    }

    public void Dispose()
    {
        lock (Gate) { if (_disposed) return; _disposed = true; }
        if (_capture != null && _previewStarted)
        {
            var result = _capture->StopPreview();
            if (result.Failed) Console.Error.WriteLine($"StopPreview failed: {result}");
        }
        if (_engine != null)
        {
            var result = _engine->Shutdown();
            if (result.Failed) Console.Error.WriteLine($"MediaEngine shutdown failed: {result}");
        }
        VideoDevice.Release(_preview); _preview = null;
        VideoDevice.Release(_capture); _capture = null;
        VideoDevice.Release(_engine); _engine = null;
        VideoDevice.Release(_pending); _pending = null;
        VideoDevice.Release(_reader); _reader = null;
        VideoDevice.Release(_upload); _upload = null;
        VideoDevice.Release(_manager); _manager = null;
        VideoDevice.Release(_manager9); _manager9 = null;
        _samples?.Dispose(); _events?.Dispose();
        Graphics.Dispose();
        if (_mfStarted)
        {
            var result = PInvoke.MFShutdown();
            if (result.Failed) Console.Error.WriteLine($"MFShutdown failed: {result}");
            _mfStarted = false;
        }
    }
}
