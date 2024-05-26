using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static LiwhallyawhuleLaqarhifehawhedem.PulseAudio.PulseAudioVolumeManager.PInvoke;

namespace LiwhallyawhuleLaqarhifehawhedem.PulseAudio
{
    /// <summary>
    /// “脉冲”音量管理，这是基于 PulseAudio 的封装
    /// </summary>
    [SupportedOSPlatform("linux")]
    public partial class PulseAudioVolumeManager // : IAudioVolumeManager
    {
        private TaskCompletionSource<bool>? _initTaskCompletionSource;
        private IntPtr _mainLoop;
        private IntPtr _context;
        private readonly pa_context_subscribe_cb_t _contextSubscribeCallback;

        private int? _volume;
        private bool? _mute;

        private readonly string _applicationName;

        public event EventHandler<int>? VolumeChanged;
        public event EventHandler<bool>? MuteChanged;

        /// <summary>
        /// 创建“脉冲”音量管理
        /// </summary>
        /// <param name="applicationName">应用名，可选，只是用于调用 pa_context_new 时传入，无特别含义和作用</param>
        public PulseAudioVolumeManager(string? applicationName = null)
        {
            _applicationName = applicationName ?? Path.GetRandomFileName().Replace('.', '_');
            _contextSubscribeCallback = ContextSubscribeCallback;
        }

        public async Task<bool> Init()
        {
            if (_initTaskCompletionSource == null)
            {
                bool isReady = false;
                _initTaskCompletionSource = new TaskCompletionSource<bool>();

                _mainLoop = pa_threaded_mainloop_new();
                if (_mainLoop != IntPtr.Zero)
                {
                    var mainloopApi = pa_threaded_mainloop_get_api(_mainLoop);
                    if (mainloopApi != IntPtr.Zero)
                    {
                        var context = pa_context_new(mainloopApi, _applicationName);
                        if (context != IntPtr.Zero)
                        {
                            pa_context_set_state_callback(context, ContextStateCallback, IntPtr.Zero);

                            await Task.Run(() =>
                            {
                                var result = pa_context_connect(context, IntPtr.Zero, 0, IntPtr.Zero);
                                if (result < 0)
                                {
                                    return;
                                }

                                result = pa_threaded_mainloop_start(_mainLoop);
                                if (result < 0)
                                {
                                    return;
                                }

                                while (true)
                                {
                                    var state = pa_context_get_state(context);

                                    if (state == pa_context_state_t.PA_CONTEXT_READY)
                                    {
                                        isReady = true;
                                        _context = context;
                                        break;
                                    }

                                    if (!PA_CONTEXT_IS_GOOD(state))
                                    {
                                        return;
                                    }

                                    pa_threaded_mainloop_wait(_mainLoop);
                                }
                            });
                        }
                    }
                }

                //Log.Info($"[PulseAudioVolumeManager.Init]: {isReady}");
                _initTaskCompletionSource.SetResult(isReady);
                return isReady;
            }
            else
            {
                return await _initTaskCompletionSource.Task;
            }
        }

        private void ContextStateCallback(IntPtr c, IntPtr userdata)
        {
            switch (pa_context_get_state(c))
            {
                case pa_context_state_t.PA_CONTEXT_READY:
                    pa_context_set_subscribe_callback(c, _contextSubscribeCallback, IntPtr.Zero);
                    var op = pa_context_subscribe(c, pa_subscription_mask_t.PA_SUBSCRIPTION_MASK_SINK, IntPtr.Zero, IntPtr.Zero);
                    pa_operation_unref(op);
                    pa_threaded_mainloop_signal(_mainLoop, 0);
                    break;

                case pa_context_state_t.PA_CONTEXT_TERMINATED:
                case pa_context_state_t.PA_CONTEXT_FAILED:
                    pa_threaded_mainloop_signal(_mainLoop, 0);
                    break;
            }
        }

        private void ContextSubscribeCallback(IntPtr c, pa_subscription_event_type_t t, uint idx, IntPtr userdata)
        {
            if ((t & pa_subscription_event_type_t.PA_SUBSCRIPTION_EVENT_FACILITY_MASK) == pa_subscription_event_type_t.PA_SUBSCRIPTION_EVENT_SINK)
            {
                Task.Run(() =>
                {
                    int? volume = null;
                    bool? mute = null;

                    pa_threaded_mainloop_lock(_mainLoop);

                    var sinkName = GetDefaultSinkName(c);
                    if (sinkName != null)
                    {
                        var info = GetSinkInfo(c, sinkName);
                        volume = GetVolumeValue(info.volume);
                        mute = info.mute;
                    }

                    pa_threaded_mainloop_unlock(_mainLoop);

                    if (volume is int volumeVal && volumeVal != _volume)
                    {
                        _volume = volumeVal;
                        VolumeChanged?.Invoke(this, volumeVal);
                    }
                    if (mute is bool muteVal && muteVal != _mute)
                    {
                        _mute = muteVal;
                        MuteChanged?.Invoke(this, muteVal);
                    }
                });
            }
        }

        private void ServerInfoCallback(IntPtr c, in pa_server_info i, IntPtr userdata)
        {
            unsafe
            {
#pragma warning disable CS8500
                *(string*) userdata = Marshal.PtrToStringUTF8(i.default_sink_name);
#pragma warning restore CS8500
                pa_threaded_mainloop_signal(_mainLoop, 0);
            }
        }

        private void SinkInfoCallback(IntPtr c, in pa_sink_info i, int eol, IntPtr userdata)
        {
            if (eol != 0)
            {
                pa_threaded_mainloop_signal(_mainLoop, 0);
                return;
            }

            unsafe
            {
                *((pa_cvolume, bool)*) userdata = (i.volume, i.mute != 0);
            }
        }

        public async Task<bool> GetMute()
        {
            bool result = false;
            if (_context != IntPtr.Zero)
            {
                await Task.Run(() =>
                {
                    pa_threaded_mainloop_lock(_mainLoop);

                    var sinkName = GetDefaultSinkName(_context);
                    if (sinkName != null)
                    {
                        var (volume, mute) = GetSinkInfo(_context, sinkName);
                        result = mute;
                    }

                    pa_threaded_mainloop_unlock(_mainLoop);
                });
            }
            return result;
        }

        public async Task<int> GetVolume()
        {
            int result = 50;
            if (_context != IntPtr.Zero)
            {
                await Task.Run(() =>
                {
                    pa_threaded_mainloop_lock(_mainLoop);

                    var sinkName = GetDefaultSinkName(_context);
                    if (sinkName != null)
                    {
                        var (volume, mute) = GetSinkInfo(_context, sinkName);
                        result = GetVolumeValue(volume);
                    }

                    pa_threaded_mainloop_unlock(_mainLoop);
                });
            }
            return result;
        }

        public async Task SetMute(bool mute)
        {
            if (_context != IntPtr.Zero)
            {
                await Task.Run(() =>
                {
                    pa_threaded_mainloop_lock(_mainLoop);

                    var sinkName = GetDefaultSinkName(_context);
                    if (sinkName != null)
                    {
                        pa_context_set_sink_mute_by_name(_context, sinkName, mute ? 1 : 0, IntPtr.Zero, IntPtr.Zero);
                    }

                    pa_threaded_mainloop_unlock(_mainLoop);
                });
            }
        }

        public async Task SetVolume(int volume)
        {
            if (_context != IntPtr.Zero)
            {
                await Task.Run(() =>
                {
                    pa_threaded_mainloop_lock(_mainLoop);

                    var sinkName = GetDefaultSinkName(_context);
                    if (sinkName != null)
                    {
                        var info = GetSinkInfo(_context, sinkName);

                        pa_cvolume_set(ref info.volume, info.volume.channels, (uint) (volume * 65536 / 100));

                        pa_context_set_sink_volume_by_name(_context, sinkName, info.volume, IntPtr.Zero, IntPtr.Zero);
                    }

                    pa_threaded_mainloop_unlock(_mainLoop);
                });
            }
        }

        /// <summary>
        /// 获取默认的输出设备名称
        /// 这个方法要在 pa_threaded_mainloop_lock 和 pa_threaded_mainloop_unlock 之间调用
        /// </summary>
        /// <returns></returns>
        private string? GetDefaultSinkName(IntPtr context)
        {
            unsafe
            {
                string? sinkName = null;

                // 取 sinkName 地址，相当于 ref string 用法，在 ServerInfoCallback 给 sinkName 赋值
                var op = pa_context_get_server_info(context, ServerInfoCallback, (IntPtr) (&sinkName));
                while (pa_operation_get_state(op) == pa_operation_state_t.PA_OPERATION_RUNNING)
                {
                    pa_threaded_mainloop_wait(_mainLoop);
                }
                pa_operation_unref(op);
                return sinkName;
            }
        }

        /// <summary>
        /// 获取默认的输出设备信息
        /// 这个方法要在 pa_threaded_mainloop_lock 和 pa_threaded_mainloop_unlock 之间调用
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sinkName"></param>
        /// <returns></returns>
        private (pa_cvolume volume, bool mute) GetSinkInfo(IntPtr context, string sinkName)
        {
            unsafe
            {
                (pa_cvolume, bool) info;
                var op = pa_context_get_sink_info_by_name(context, sinkName, SinkInfoCallback, (IntPtr) (&info));
                while (pa_operation_get_state(op) == pa_operation_state_t.PA_OPERATION_RUNNING)
                {
                    pa_threaded_mainloop_wait(_mainLoop);
                }
                pa_operation_unref(op);
                return info;
            }
        }

        private int GetVolumeValue(in pa_cvolume volume)
        {
            var val = pa_cvolume_avg(volume);
            return (int) Math.Round((val * 100d / 65536));
        }
    }
}
