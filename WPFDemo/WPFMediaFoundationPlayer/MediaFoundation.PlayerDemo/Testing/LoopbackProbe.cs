using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;

namespace MediaFoundation.PlayerDemo;

internal sealed unsafe class LoopbackProbe : IDisposable
{
    private IAudioClient* _client;
    private IAudioCaptureClient* _capture;
    private int _channels;
    private bool _floating;
    internal long Samples { get; private set; }
    private double _energy;
    internal double Rms => Samples == 0 ? 0 : Math.Sqrt(_energy / Samples);

    internal LoopbackProbe()
    {
        IMMDeviceEnumerator* enumerator = null;
        IMMDevice* device = null;
        WAVEFORMATEX* format = null;
        try
        {
            Guid clsid = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
            Guid iid = IMMDeviceEnumerator.IID_Guid;
            PInvoke.CoCreateInstance
                (&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)&enumerator).ThrowOnFailure();
            enumerator->GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, &device);
            iid = IAudioClient.IID_Guid;
            IAudioClient* activated = null;
            device->Activate(&iid, CLSCTX.CLSCTX_INPROC_SERVER, null, (void**)&activated);
            _client = activated;
            _client->GetMixFormat(&format);
            _channels = format->nChannels;
            _floating = format->wFormatTag == 3 || (format->wFormatTag == 0xFFFE &&
                                                   *(Guid*)((byte*)format + 24) == new Guid
                                                       ("00000003-0000-0010-8000-00aa00389b71"));
            if (!(_floating && format->wBitsPerSample == 32) &&
                !(format->wFormatTag == 1 && format->wBitsPerSample == 16))
                throw new NotSupportedException
                    ($"Loopback format tag={format->wFormatTag} bits={format->wBitsPerSample}");
            _client->Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 0x00020000, 10000000, 0, format, null);
            iid = IAudioCaptureClient.IID_Guid;
            IAudioCaptureClient* service = null;
            _client->GetService(&iid, (void**)&service);
            _capture = service;
            _client->Start();
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            if (format != null) PInvoke.CoTaskMemFree(format);
            if (device != null) device->Release();
            if (enumerator != null) enumerator->Release();
        }
    }

    internal void Poll()
    {
        uint packet = 0;
        _capture->GetNextPacketSize(&packet);
        while (packet != 0)
        {
            byte* data = null;
            uint frames = 0, flags = 0;
            _capture->GetBuffer(&data, &frames, &flags, null, null);
            try
            {
                int count = checked((int)frames * _channels);
                for (int i = 0; i < count; i++)
                {
                    double sample = (flags & 2) != 0 ? 0 : _floating ? ((float*)data)[i] : ((short*)data)[i] / 32768.0;
                    _energy += sample * sample;
                }

                Samples += count;
            }
            finally
            {
                _capture->ReleaseBuffer(frames);
            }

            _capture->GetNextPacketSize(&packet);
        }
    }

    /// <summary>释放回环音频捕获使用的原生客户端。</summary>
    public void Dispose()
    {
        if (_capture != null)
        {
            _capture->Release();
            _capture = null;
        }

        if (_client != null)
        {
            _client->Release();
            _client = null;
        }
    }
}
