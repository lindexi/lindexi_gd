using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.DirectX.ComInterfaces;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.Extensions.Winmm;
using Lsj.Util.Win32.MediaFoundation.ComInterfaces;
using Lsj.Util.Win32.NativeUI.DirectX;
using Lsj.Util.Win32.Structs;
using System;
using System.Diagnostics;
using static Lsj.Util.Win32.ComInterfaces.IIDs;
using static Lsj.Util.Win32.Constants;
using static Lsj.Util.Win32.DirectX.Enums.D3DBACKBUFFER_TYPE;
using static Lsj.Util.Win32.DirectX.Enums.D3DTEXTUREFILTERTYPE;
using static Lsj.Util.Win32.Dxva2;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MF_SOURCE_READER_FLAG;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.AudioSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MajorMediaTypes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MediaFoundationAttributes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.VideoSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.Mf;
using static Lsj.Util.Win32.MediaFoundation.Mfapi;
using static Lsj.Util.Win32.MediaFoundation.Mfplat;
using static Lsj.Util.Win32.MediaFoundation.Mfreadwrite;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;

namespace D3D9MediaFoundation
{
    public class PlayerWindow : D3D9ExWindow
    {
        public P<IMFSourceReader> Reader { get; private set; }

        public P<IDirect3DDeviceManager9> DeviceManager { get; private set; }

        public uint VideoWidth { get; private set; }

        public uint VideoHeight { get; private set; }

        public Stopwatch PlayerStopwatch { get; } = new Stopwatch();

        public WaveOutPlayer? AudioPlayer { get; private set; }

        protected override bool OnAfterInitD3D9Ex(HWND hWnd)
        {
            var result = MFCreateAttributes(out var attr, 2);
            if (!result)
            {
                return false;
            }

            if (!DXVA2CreateDirect3DDeviceManager9(out var resetToken, out var deviceManager))
            {
                return false;
            }

            if (!deviceManager.Value.ResetDevice(Device, resetToken))
            {
                deviceManager.AsIUnknown().Value.Release();
                return false;
            }

            DeviceManager = deviceManager;


            attr.Value.SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);
            attr.Value.SetUnknown(MF_SOURCE_READER_D3D_MANAGER, deviceManager.AsIUnknown());

            result = MFCreateSourceReaderFromURL(Program.VideoPath, attr, out var reader);
            attr.AsIUnknown().Value.Release();

            if (!result)
            {
                return false;
            }

            Reader = reader;

            result = MFCreateMediaType(out var videoTypeOut);
            if (!result)
            {
                return false;
            }

            result = MFCreateMediaType(out var audioTypeOut);
            if (!result)
            {
                return false;
            }

            videoTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
            videoTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_SUBTYPE, MFVideoFormat_NV12);
            Reader.Value.SetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, ref NullRef<DWORD>(), videoTypeOut);
            videoTypeOut.AsIUnknown().Value.Release();

            audioTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);
            audioTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM);
            audioTypeOut.AsP<IMFAttributes>().Value.SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16);
            audioTypeOut.AsP<IMFAttributes>().Value.SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, 44100);
            audioTypeOut.AsP<IMFAttributes>().Value.SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, 2);
            Reader.Value.SetCurrentMediaType(MF_SOURCE_READER_FIRST_AUDIO_STREAM, ref NullRef<DWORD>(), audioTypeOut);
            audioTypeOut.AsIUnknown().Value.Release();

            Reader.Value.GetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, out var actualType);
            MFGetAttributeSize(actualType.AsP<IMFAttributes>(), MF_MT_FRAME_SIZE, out var width, out var height);
            actualType.AsIUnknown().Value.Release();
            VideoWidth = width;
            VideoHeight = height;

            PlayerStopwatch.Restart();

            return true;
        }


        private long _lastTimeNs = 0; // ns
        private long _sentAudioTimeNs = 0; // ns
        private long _offsetTimeNs = 0;

        private void RenderFrame()
        {
            var elapsedNs = PlayerStopwatch.Elapsed.TotalNanoseconds + _offsetTimeNs;
            if (elapsedNs < _lastTimeNs)
            {
                return;
            }
            var result = Reader.Value.ReadSample(MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, out var actualStreamIndex, out var flags, out var timestamp, out var sample);
            _lastTimeNs = timestamp * 100;
            AudioPlayer?.SyncCurrentTime(_lastTimeNs);

            if (flags.HasFlag(MF_SOURCE_READERF_ENDOFSTREAM))
            {
                var propVariant = new PROPVARIANT();
                propVariant.vt = VARENUM.VT_I8;
                propVariant.lVal = 0;
                _sentAudioTimeNs = 0;
                AudioPlayer?.Reset();
                result = Reader.Value.SetCurrentPosition(new GUID(), propVariant);
                PlayerStopwatch.Restart();
                _offsetTimeNs = 0;
                return;
            }

            if (_lastTimeNs < elapsedNs)
            {
                _offsetTimeNs += _lastTimeNs - (long)elapsedNs;
                Console.WriteLine($"Update _offsetTimeNs {_offsetTimeNs}");
            }

            if (sample != NULL)
            {
                result = sample.Value.ConvertToContiguousBuffer(out var buffer);

                if (result)
                {
                    var MR_BUFFER_SERVICE = (GUID)"{A562248C-9AC6-4FFC-9FBA-3AF8F8AD1A4D}";
                    result = MFGetService(buffer.AsP<IUnknown>(), MR_BUFFER_SERVICE, IID_IDirect3DSurface9, out var surfacePtr);
                    if (result)
                    {
                        P<IDirect3DSurface9> surface = (IntPtr)surfacePtr;
                        surface.Value.GetDesc(out var desc);

                        result = Device.Value.GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, out var backBuffer);
                        if (result)
                        {
                            result = Device.Value.StretchRect(surface, NullRef<RECT>(), backBuffer, NullRef<RECT>(), D3DTEXF_NONE);
                        }

                        Device.Value.Present(NullRef<RECT>(), NullRef<RECT>(), NULL, NullRef<RGNDATA>());

                        surface.AsIUnknown().Value.Release();
                        backBuffer.AsIUnknown().Value.Release();
                    }

                    buffer.AsIUnknown().Value.Release();
                }

                sample.AsIUnknown().Value.Release();
            }

        }

        protected override LRESULT OnPaint(HWND hWnd, WPARAM wParam, LPARAM lParam)
        {
            RenderFrame();
            return 0;
        }
    }
}
