using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.MediaFoundation.ComInterfaces;
using Lsj.Util.Win32.MediaFoundation.Enums;
using System.Runtime.InteropServices;

namespace WPFD3D11MediaEngine
{
    class MediaEngineCallback : BaseComObject<IMFMediaEngineNotify>
    {
        private readonly MainWindow _window;

        public MediaEngineCallback(MainWindow window)
        {
            _window = window;
        }

        protected override int VTableItemCount => 4;

        protected override void InitVTable(VTableInitHelper<IMFMediaEngineNotify> vTable)
        {
            vTable.SetMethod(3, GetMethodInfo(nameof(EventNotify)));
        }

        public override HRESULT QueryInterface(in IID riid, out nint ppvObject)
        {
            if (riid == IIDs.IID_IMFMediaEngineNotify)
            {
                AddRef();
                ppvObject = GetNativePointer();
                return HRESULT.S_OK;
            }
            return base.QueryInterface(riid, out ppvObject);
        }

        public HRESULT EventNotify([In] MF_MEDIA_ENGINE_EVENT @event, [In] DWORD_PTR param1, [In] DWORD param2)
        {
            Console.WriteLine($"[MediaEngineCallback] {@event}");

            if (@event == MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_DURATIONCHANGE)
            {
                _window.Dispatcher.InvokeAsync(() =>
                {
                    _window.UpdateDuration();
                });
            }
            else if (@event == MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_TIMEUPDATE)
            {
                _window.Dispatcher.InvokeAsync(() =>
                {
                    _window.UpdateTime();
                });
            }

            return HRESULT.S_OK;
        }
    }
}
