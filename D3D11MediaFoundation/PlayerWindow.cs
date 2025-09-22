using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.DirectX.ComInterfaces;
using Lsj.Util.Win32.DirectX.Structs;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.Extensions.DirectX.Direct3D11;
using Lsj.Util.Win32.Extensions.Winmm;
using Lsj.Util.Win32.MediaFoundation.ComInterfaces;
using Lsj.Util.Win32.NativeUI.DirectX;
using Lsj.Util.Win32.Structs;
using System.Diagnostics;
using static Lsj.Util.Win32.Constants;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_CREATE_DEVICE_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VIDEO_FRAME_FORMAT;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VIDEO_USAGE;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VPIV_DIMENSION;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VPOV_DIMENSION;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MF_SOURCE_READER_FLAG;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.AudioSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MajorMediaTypes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MediaFoundationAttributes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.VideoSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.Mfapi;
using static Lsj.Util.Win32.MediaFoundation.Mfplat;
using static Lsj.Util.Win32.MediaFoundation.Mfreadwrite;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;

namespace D3D11MediaFoundation
{
    public class PlayerWindow : D3D11Window
    {
        public P<IMFSourceReader> Reader { get; private set; }

        public P<ID3D11VideoDevice> D3D11VideoDevice { get; private set; }

        public P<ID3D11VideoContext> D3D11VideoContext { get; private set; }

        public P<IMFDXGIDeviceManager> DXGIDeviceManager { get; private set; }

        public P<ID3D11VideoProcessorEnumerator> D3D11VideoProcessorEnumerator { get; private set; }

        public P<ID3D11VideoProcessor> D3D11VideoProcessor { get; private set; }

        public WaveOutPlayer? AudioPlayer { get; private set; }

        public uint VideoWidth { get; private set; }

        public uint VideoHeight { get; private set; }

        public Stopwatch PlayerStopwatch { get; } = new Stopwatch();

        private Thread? _audioThread;

        protected override bool OnAfterInitD3D11(HWND hWnd)
        {
            var result = MFCreateAttributes(out var attr, 2);
            if (!result)
            {
                return false;
            }

            if (!MFCreateDXGIDeviceManager(out var resetToken, out var deviceManager))
            {
                return false;
            }

            if (!deviceManager.Value.ResetDevice(D3D11Device.AsIUnknown(), resetToken))
            {
                deviceManager.AsIUnknown().Value.Release();
                return false;
            }

            DXGIDeviceManager = deviceManager;


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

            D3D11VideoDevice = D3D11Device.AsIUnknown().ToInterface<ID3D11VideoDevice>(IIDs.IID_ID3D11VideoDevice);
            D3D11VideoContext = D3D11DeviceContext.AsIUnknown().ToInterface<ID3D11VideoContext>(IIDs.IID_ID3D11VideoContext);
            D3D11VideoContext.Value.VideoProcessorSetStreamAutoProcessingMode(D3D11VideoProcessor, 0, false);
            //D3D11VideoContext.Value.VideoProcessorSetStreamFilter(D3D11VideoProcessor, 0, D3D11_VIDEO_PROCESSOR_FILTER_BRIGHTNESS, false, 0);
            //D3D11VideoContext.Value.VideoProcessorSetStreamFilter(D3D11VideoProcessor, 0, D3D11_VIDEO_PROCESSOR_FILTER_CONTRAST, false, 0);
            //D3D11VideoContext.Value.VideoProcessorSetStreamFilter(D3D11VideoProcessor, 0, D3D11_VIDEO_PROCESSOR_FILTER_HUE, false, 0);
            //D3D11VideoContext.Value.VideoProcessorSetStreamFilter(D3D11VideoProcessor, 0, D3D11_VIDEO_PROCESSOR_FILTER_SATURATION, false, 0);

            var multhread = D3D11VideoContext.AsIUnknown().ToInterface<ID3D11Multithread>(IIDs.IID_ID3D11Multithread);
            multhread.Value.SetMultithreadProtected(true);
            multhread.AsIUnknown().Value.Release();

            User32.GetWindowRect(hWnd, out var rect);
            CreateVideoProcessor(rect.right - rect.left, rect.bottom - rect.top);
            AudioPlayer = new WaveOutPlayer(2, 44100, 16);

            PlayerStopwatch.Restart();

            _audioThread = new Thread(DecodeAudio);
            _audioThread.Start();

            return true;
        }

        private long _lastTimeNs = 0; // ns
        private long _sentAudioTimeNs = 0; // ns
        private long _offsetTimeNs = 0;

        private void DecodeAudio()
        {
            if (AudioPlayer == null)
            {
                return;
            }

            //DWORD index = 0;
            //Avrt.AvSetMmThreadCharacteristics("Audio", ref index);

            bool hasStart = false;

            while (true)
            {
                if (_sentAudioTimeNs - _lastTimeNs > 200_000_000) // 200 ms
                {
                    Thread.Sleep(10);
                    continue;
                }

                var result = Reader.Value.ReadSample(MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, out var actualStreamIndex, out var flags, out var timestamp, out var sample);

                if (sample != NULL)
                {
                    result = sample.Value.ConvertToContiguousBuffer(out var buffer);
                    if (result)
                    {
                        buffer.Value.Lock(out var dataPtr, out var maxLength, out var currentLength);

                        var timeOfBuffer = currentLength * 1_000 / (44100 * 2 * 2); // ms
                        var diff = (timestamp * 100 - _sentAudioTimeNs) / 1000000; // ms

                        AudioPlayer.EnqueueAudio(dataPtr, (int)currentLength, timestamp * 100);
                        if (!hasStart)
                        {
                            hasStart = true;
                            AudioPlayer.Start();
                        }

                        buffer.Value.Unlock();

                        _sentAudioTimeNs = timestamp * 100;
                        //Console.WriteLine($"Sent Audio {_sentAudioTimeNs / 1000000} ms");

                        buffer.AsIUnknown().Value.Release();
                    }
                    sample.AsIUnknown().Value.Release();
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

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
                    var dxgiBuffer = buffer.AsIUnknown().ToInterface<IMFDXGIBuffer>(IIDs.IID_IMFDXGIBuffer, true, true);
                    dxgiBuffer.Value.GetResource(IIDs.IID_ID3D11Texture2D, out var texturePtr);
                    dxgiBuffer.Value.GetSubresourceIndex(out var index);
                    dxgiBuffer.AsIUnknown().Value.Release();

                    P<ID3D11Texture2D> texture = (nint)texturePtr;
                    //texture.Value.GetDesc(out var desc);

                    var inputDesc = new D3D11_VIDEO_PROCESSOR_INPUT_VIEW_DESC
                    {
                        ViewDimension = D3D11_VPIV_DIMENSION_TEXTURE2D,
                        Texture2D = new D3D11_TEX2D_VPIV
                        {
                            MipSlice = 0,
                            ArraySlice = index,
                        }
                    };

                    D3D11VideoDevice.Value.CreateVideoProcessorInputView(texture.AsP<ID3D11Resource>(), D3D11VideoProcessorEnumerator, inputDesc, out var inputView);

                    DXGISwapChain.Value.GetBuffer(0, IIDs.IID_ID3D11Texture2D, out var backBufferPtr);
                    P<ID3D11Texture2D> backBuffer = backBufferPtr;
                    backBuffer.Value.GetDesc(out var backDesc);
                    //Console.WriteLine($"BackBuffer {backDesc.Width}x{backDesc.Height}");

                    var outputDesc = new D3D11_VIDEO_PROCESSOR_OUTPUT_VIEW_DESC
                    {
                        ViewDimension = D3D11_VPOV_DIMENSION_TEXTURE2D,
                        Texture2D = new D3D11_TEX2D_VPOV
                        {
                            MipSlice = 0,
                        }
                    };
                    D3D11VideoDevice.Value.CreateVideoProcessorOutputView(backBuffer.AsP<ID3D11Resource>(), D3D11VideoProcessorEnumerator, outputDesc, out var outputView);

                    var stream = new D3D11_VIDEO_PROCESSOR_STREAM
                    {
                        Enable = true,
                        pInputSurface = inputView,
                    };


                    D3D11VideoContext.Value.VideoProcessorBlt(D3D11VideoProcessor, outputView, 0, 1, [stream]);
                    DXGISwapChain.Value.Present(1, 0);

                    texture.AsIUnknown().Value.Release();
                    backBuffer.AsIUnknown().Value.Release();
                    inputView.AsIUnknown().Value.Release();
                    outputView.AsIUnknown().Value.Release();
                }

                sample.AsIUnknown().Value.Release();
            }

        }


        protected override LRESULT OnPaint(HWND hWnd, WPARAM wParam, LPARAM lParam)
        {
            RenderFrame();
            return 0;
        }

        protected override LRESULT OnResize(HWND hWnd, WPARAM wParam, LPARAM lParam, WindowSizingTypes sizingType, int width, int height)
        {
            base.OnResize(hWnd, wParam, lParam, sizingType, width, height);
            if (sizingType != WindowSizingTypes.SIZE_MINIMIZED)
            {
                CreateVideoProcessor(width, height);
            }
            return 0;
        }

        private void CreateVideoProcessor(int width, int height)
        {
            var desc = new D3D11_VIDEO_PROCESSOR_CONTENT_DESC
            {
                InputFrameFormat = D3D11_VIDEO_FRAME_FORMAT_PROGRESSIVE,
                InputWidth = VideoWidth,
                InputHeight = VideoHeight,
                OutputWidth = (uint)width,
                OutputHeight = (uint)height,
                Usage = D3D11_VIDEO_USAGE_PLAYBACK_NORMAL,
            };
            Console.WriteLine($"Input {VideoWidth}x{VideoHeight} Ouput {width}x{height}");
            D3D11VideoDevice.Value.CreateVideoProcessorEnumerator(desc, out var enumerator);
            D3D11VideoProcessorEnumerator = enumerator;

            D3D11VideoDevice.Value.CreateVideoProcessor(D3D11VideoProcessorEnumerator, 0, out var processor);
            D3D11VideoProcessor = processor;

            var sourceRect = new RECT
            {
                left = 0,
                top = 0,
                right = (int)VideoWidth,
                bottom = (int)VideoHeight,
            };
            D3D11VideoContext.Value.VideoProcessorSetStreamSourceRect(D3D11VideoProcessor, 0, true, sourceRect);

            var destRect = new RECT
            {
                left = 0,
                top = 0,
                right = width,
                bottom = height,
            };
            D3D11VideoContext.Value.VideoProcessorSetStreamDestRect(D3D11VideoProcessor, 0, true, destRect);
            D3D11VideoContext.Value.VideoProcessorSetOutputTargetRect(D3D11VideoProcessor, true, destRect);
        }

        protected override P<ID3D11Device> CreateD3D11Device()
        {
#if DEBUG
            DeviceHelper.CreateDevice(out var device, out NullRef<P<ID3D11DeviceContext>>(), D3D11_CREATE_DEVICE_DEBUG);
            return device;
#endif
            return base.CreateD3D11Device();
        }
    }
}
