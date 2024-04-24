using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LiwhallyawhuleLaqarhifehawhedem.PulseAudio
{
    public partial class PulseAudioVolumeManager
    {
        public static class PInvoke
        {
            public enum pa_context_state_t
            {
                PA_CONTEXT_UNCONNECTED,
                PA_CONTEXT_CONNECTING,
                PA_CONTEXT_AUTHORIZING,
                PA_CONTEXT_SETTING_NAME,
                PA_CONTEXT_READY,
                PA_CONTEXT_FAILED,
                PA_CONTEXT_TERMINATED,
            }

            [Flags]
            public enum pa_subscription_event_type_t : uint
            {
                PA_SUBSCRIPTION_EVENT_SINK = 0x0000U,
                PA_SUBSCRIPTION_EVENT_SOURCE = 0x0001U,
                PA_SUBSCRIPTION_EVENT_SINK_INPUT = 0x0002U,
                PA_SUBSCRIPTION_EVENT_SOURCE_OUTPUT = 0x0003U,
                PA_SUBSCRIPTION_EVENT_MODULE = 0x0004U,
                PA_SUBSCRIPTION_EVENT_CLIENT = 0x0005U,
                PA_SUBSCRIPTION_EVENT_SAMPLE_CACHE = 0x0006U,
                PA_SUBSCRIPTION_EVENT_SERVER = 0x0007U,
                PA_SUBSCRIPTION_EVENT_AUTOLOAD = 0x0008U,
                PA_SUBSCRIPTION_EVENT_CARD = 0x0009U,
                PA_SUBSCRIPTION_EVENT_FACILITY_MASK = 0x000FU,
                PA_SUBSCRIPTION_EVENT_NEW = 0x0000U,
                PA_SUBSCRIPTION_EVENT_CHANGE = 0x0010U,
                PA_SUBSCRIPTION_EVENT_REMOVE = 0x0020U,
                PA_SUBSCRIPTION_EVENT_TYPE_MASK = 0x0030U,
            }

            public enum pa_subscription_mask_t : uint
            {
                PA_SUBSCRIPTION_MASK_NULL = 0x0000U,
                PA_SUBSCRIPTION_MASK_SINK = 0x0001U,
                PA_SUBSCRIPTION_MASK_SOURCE = 0x0002U,
                PA_SUBSCRIPTION_MASK_SINK_INPUT = 0x0004U,
                PA_SUBSCRIPTION_MASK_SOURCE_OUTPUT = 0x0008U,
                PA_SUBSCRIPTION_MASK_MODULE = 0x0010U,
                PA_SUBSCRIPTION_MASK_CLIENT = 0x0020U,
                PA_SUBSCRIPTION_MASK_SAMPLE_CACHE = 0x0040U,
                PA_SUBSCRIPTION_MASK_SERVER = 0x0080U,
                PA_SUBSCRIPTION_MASK_AUTOLOAD = 0x0100U,
                PA_SUBSCRIPTION_MASK_CARD = 0x0200U,
                PA_SUBSCRIPTION_MASK_ALL = 0x02ffU,
            }

            public enum pa_sample_format_t
            {
                PA_SAMPLE_U8,
                PA_SAMPLE_ALAW,
                PA_SAMPLE_ULAW,
                PA_SAMPLE_S16LE,
                PA_SAMPLE_S16BE,
                PA_SAMPLE_FLOAT32LE,
                PA_SAMPLE_FLOAT32BE,
                PA_SAMPLE_S32LE,
                PA_SAMPLE_S32BE,
                PA_SAMPLE_S24LE,
                PA_SAMPLE_S24BE,
                PA_SAMPLE_S24_32LE,
                PA_SAMPLE_S24_32BE,
                PA_SAMPLE_MAX,
                PA_SAMPLE_INVALID = -1
            }

            public enum pa_operation_state_t
            {
                PA_OPERATION_RUNNING,
                PA_OPERATION_DONE,
                PA_OPERATION_CANCELLED,
            }


            [StructLayout(LayoutKind.Sequential)]
            public struct pa_server_info
            {
                public IntPtr user_name;
                public IntPtr host_name;
                public IntPtr server_version;
                public IntPtr server_name;
                pa_sample_spec sample_spec;
                public IntPtr default_sink_name;
                public IntPtr default_source_name;
                public uint cookie;
                pa_channel_map channel_map;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct pa_sample_spec
            {
                public pa_sample_format_t format;
                public uint rate;
                public byte channels;
            }

            [StructLayout(LayoutKind.Sequential, Size = 132)]
            public struct pa_channel_map
            {
                public byte channels;
                //public pa_channel_position_t map[PA_CHANNELS_MAX];
            }

            [StructLayout(LayoutKind.Sequential, Size = 132)]
            public struct pa_cvolume
            {
                public byte channels;
                //public pa_volume_t values[PA_CHANNELS_MAX];
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct pa_sink_info
            {
                public IntPtr name;
                public uint index;
                public IntPtr description;
                public pa_sample_spec sample_spec;
                public pa_channel_map channel_map;
                public uint owner_module;
                public pa_cvolume volume;
                public int mute;
                public uint monitor_source;
                public IntPtr monitor_source_name;
                public ulong latency;
                public IntPtr driver;
                public uint flags;
                public IntPtr proplist;
                public ulong configured_latency;
                public uint base_volume;
                public uint state;
                public uint n_volume_steps;
                public uint card;
                public uint n_ports;
                public IntPtr ports;
                public IntPtr active_port;
                public byte n_formats;
                public IntPtr formats;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool PA_CONTEXT_IS_GOOD(pa_context_state_t x)
            {
                return x == pa_context_state_t.PA_CONTEXT_CONNECTING || x == pa_context_state_t.PA_CONTEXT_AUTHORIZING ||
                    x == pa_context_state_t.PA_CONTEXT_SETTING_NAME || x == pa_context_state_t.PA_CONTEXT_READY;
            }

            public delegate void pa_context_notify_cb_t(IntPtr c, IntPtr userdata);

            public delegate void pa_context_subscribe_cb_t(IntPtr c, pa_subscription_event_type_t t, uint idx, IntPtr userdata);

            public delegate void pa_server_info_cb_t(IntPtr c, in pa_server_info i, IntPtr userdata);

            public delegate void pa_sink_info_cb_t(IntPtr c, in pa_sink_info i, int eol, IntPtr userdata);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_threaded_mainloop_new();

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_threaded_mainloop_get_api(IntPtr m);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_context_new(IntPtr mainloop, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

            [DllImport("libpulse.so.0")]
            public static extern void pa_context_set_state_callback(IntPtr c, pa_context_notify_cb_t cb, IntPtr userdata);

            [DllImport("libpulse.so.0")]
            public static extern int pa_context_connect(IntPtr c, IntPtr server, uint flags, IntPtr api);

            [DllImport("libpulse.so.0")]
            public static extern int pa_threaded_mainloop_start(IntPtr m);

            [DllImport("libpulse.so.0")]
            public static extern pa_context_state_t pa_context_get_state(IntPtr c);

            [DllImport("libpulse.so.0")]
            public static extern void pa_threaded_mainloop_wait(IntPtr m);

            [DllImport("libpulse.so.0")]
            public static extern void pa_context_set_subscribe_callback(IntPtr c, pa_context_subscribe_cb_t cb, IntPtr userdata);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_context_subscribe(IntPtr c, pa_subscription_mask_t m, IntPtr cb, IntPtr userdata);

            [DllImport("libpulse.so.0")]
            public static extern void pa_operation_unref(IntPtr o);

            [DllImport("libpulse.so.0")]
            public static extern void pa_threaded_mainloop_signal(IntPtr m, int wait_for_accept);

            [DllImport("libpulse.so.0")]
            public static extern void pa_threaded_mainloop_lock(IntPtr m);

            [DllImport("libpulse.so.0")]
            public static extern void pa_threaded_mainloop_unlock(IntPtr m);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_context_get_server_info(IntPtr c, pa_server_info_cb_t cb, IntPtr userdata);

            [DllImport("libpulse.so.0")]
            public static extern pa_operation_state_t pa_operation_get_state(IntPtr o);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_context_get_sink_info_by_name(IntPtr c, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, pa_sink_info_cb_t cb, IntPtr userdata);

            [DllImport("libpulse.so.0")]
            public static extern uint pa_cvolume_avg(in pa_cvolume a);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_cvolume_set(ref pa_cvolume a, uint channels, uint v);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_context_set_sink_mute_by_name(IntPtr c, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int mute, IntPtr cb, IntPtr userdata);

            [DllImport("libpulse.so.0")]
            public static extern IntPtr pa_context_set_sink_volume_by_name(IntPtr c, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, in pa_cvolume volume, IntPtr cb, IntPtr userdata);
        }
    }
}
