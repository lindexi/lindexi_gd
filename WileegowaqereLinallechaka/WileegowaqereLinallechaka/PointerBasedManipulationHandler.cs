using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DirectManipulation;

namespace WileegowaqereLinallechaka
{
    class PointerBasedManipulationHandler : IDirectManipulationViewportEventHandler, IDisposable
    {
        const int ContentMatrixSize = 6;
        // Actual values don't matter
        static tagRECT DefaultViewport = new tagRECT { top = 0, left = 0, right = 1000, bottom = 1000 };

        HwndSource hwndSource;
        IDirectManipulationManager manager;
        IDirectManipulationViewport viewport;
        Size viewportSize;

        uint viewportEventHandlerRegistration;
        float lastScale;
        float[] matrix = new float[ContentMatrixSize];
        IntPtr matrixContent;

        float lastTranslationX, lastTranslationY;

        public PointerBasedManipulationHandler()
        {
            matrixContent = Marshal.AllocCoTaskMem(sizeof(float) * ContentMatrixSize);
        }

        public event Action<float> ScaleUpdated;
        public event Action<float, float> TranslationUpdated;

        public HwndSource HwndSource
        {
            get => hwndSource;
            set
            {
                var first = hwndSource == null && value != null;
                var oldHwndSource = hwndSource;
                if (oldHwndSource != null)
                    oldHwndSource.RemoveHook(WndProcHook);
                if (value != null)
                    value.AddHook(WndProcHook);
                this.hwndSource = value;
                if (first && value != null)
                    InitializeDirectManipulation();
            }
        }

        IntPtr Window => hwndSource.Handle;

        public void InitializeDirectManipulation()
        {
            this.manager = (IDirectManipulationManager)Activator.CreateInstance(typeof(DirectManipulationManagerClass));
            var riid = typeof(IDirectManipulationUpdateManager).GUID;
            riid = typeof(IDirectManipulationViewport).GUID;
            this.viewport = manager.CreateViewport(null, Window, ref riid) as IDirectManipulationViewport;

            var configuration = DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_INTERACTION
                | DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_SCALING
                | DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_TRANSLATION_X
                | DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_TRANSLATION_Y
                | DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_TRANSLATION_INERTIA;
            viewport.ActivateConfiguration(configuration);
            viewportEventHandlerRegistration = viewport.AddEventHandler(Window, this);
            viewport.SetViewportRect(ref DefaultViewport);
            viewport.Enable();
        }

        public void Dispose()
        {
            viewport.RemoveEventHandler(viewportEventHandlerRegistration);
            Marshal.FreeCoTaskMem(matrixContent);
            HwndSource = null;
        }

        public void SetSize(Size size)
        {
            this.viewportSize = size;
            viewport.Stop();
            var rect = new tagRECT
            {
                left = 0,
                top = 0,
                right = (int)size.Width,
                bottom = (int)size.Height
            };
            viewport.SetViewportRect(ref rect);
        }

        // Our custom hook to process WM_POINTER event
        IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_POINTERDOWN || msg == DM_POINTERHITTEST)
            {
                var pointerID = GetPointerId(wParam);
                var pointerInfo = default(POINTER_INFO);
                if (!GetPointerInfo(pointerID, ref pointerInfo))
                    return IntPtr.Zero;
                if (pointerInfo.pointerType != POINTER_INPUT_TYPE.PT_TOUCHPAD &&
                    pointerInfo.pointerType != POINTER_INPUT_TYPE.PT_TOUCH)
                    return IntPtr.Zero;

                viewport.SetContact(pointerID);
            }
            else if (msg == WM_SIZE && manager != null)
            {
                if (wParam == SIZE_MAXHIDE
                    || wParam == SIZE_MINIMIZED)
                    manager.Deactivate(Window);
                else
                    manager.Activate(Window);
            }
            return IntPtr.Zero;
        }

        void ResetViewport(IDirectManipulationViewport viewport)
        {
            viewport.ZoomToRect(0, 0, (float)viewportSize.Width, (float)viewportSize.Height, 0);
            lastScale = 1.0f;
            lastTranslationX = lastTranslationY = 0;
        }

        public void OnViewportStatusChanged([In, MarshalAs(UnmanagedType.Interface)] IDirectManipulationViewport viewport, [In] DIRECTMANIPULATION_STATUS current, [In] DIRECTMANIPULATION_STATUS previous)
        {
            if (previous == current)
                return;

            if (current == DIRECTMANIPULATION_STATUS.DIRECTMANIPULATION_READY)
                ResetViewport(viewport);
        }

        public void OnViewportUpdated([In, MarshalAs(UnmanagedType.Interface)] IDirectManipulationViewport viewport)
        {
        }

        public void OnContentUpdated([In, MarshalAs(UnmanagedType.Interface)] IDirectManipulationViewport viewport, [In, MarshalAs(UnmanagedType.Interface)] IDirectManipulationContent content)
        {
            content.GetContentTransform(matrixContent, ContentMatrixSize);
            Marshal.Copy(matrixContent, matrix, 0, ContentMatrixSize);

            float scale = matrix[0];
            float newX = matrix[4];
            float newY = matrix[5];

            if (scale == 0.0f)
                return;

            var deltaX = (newX - lastTranslationX);
            var deltaY = (newY - lastTranslationY);

            bool ShallowFloatEquals(float f1, float f2)
                => Math.Abs(f2 - f1) < float.Epsilon;

            if ((ShallowFloatEquals(scale, 1.0f) || ShallowFloatEquals(scale, lastScale))
                && (Math.Abs(deltaX) > 1.0f || Math.Abs(deltaY) > 1.0f))
            {
                TranslationUpdated?.Invoke(-deltaX, -deltaY);
            }
            else if (!ShallowFloatEquals(lastScale, scale))
            {
                ScaleUpdated?.Invoke(scale);
            }

            lastScale = scale;
            lastTranslationX = newX;
            lastTranslationY = newY;
        }

        #region Native methods
        const int WM_POINTERDOWN = 0x0246;
        const int DM_POINTERHITTEST = 0x0250;
        const int WM_SIZE = 0x0005;

        static readonly IntPtr SIZE_MAXHIDE = new IntPtr(4);
        static readonly IntPtr SIZE_MINIMIZED = new IntPtr(1);

        uint GetPointerId(IntPtr wParam) => (uint)(unchecked((int)wParam.ToInt64()) & 0xFFFF);

        [DllImport("user32.dll", EntryPoint = "GetPointerInfo", SetLastError = true)]
        internal static extern bool GetPointerInfo([In] uint pointerId, [In, Out] ref POINTER_INFO pointerInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINTER_INFO
        {
            internal POINTER_INPUT_TYPE pointerType;
            internal uint pointerId;
            internal uint frameId;
            internal POINTER_FLAGS pointerFlags;
            internal IntPtr sourceDevice;
            internal IntPtr hwndTarget;
            internal POINT ptPixelLocation;
            internal POINT ptHimetricLocation;
            internal POINT ptPixelLocationRaw;
            internal POINT ptHimetricLocationRaw;
            internal uint dwTime;
            internal uint historyCount;
            internal int inputData;
            internal uint dwKeyStates;
            internal ulong PerformanceCount;
            internal POINTER_BUTTON_CHANGE_TYPE ButtonChangeType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINT
        {
            internal int X;
            internal int Y;
        }

        internal enum POINTER_INPUT_TYPE : uint
        {
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003,
            PT_MOUSE = 0x00000004,
            PT_TOUCHPAD = 0x00000005
        }

        [Flags]
        internal enum POINTER_FLAGS : uint
        {
            POINTER_FLAG_NONE = 0x00000000,
            POINTER_FLAG_NEW = 0x00000001,
            POINTER_FLAG_INRANGE = 0x00000002,
            POINTER_FLAG_INCONTACT = 0x00000004,
            POINTER_FLAG_FIRSTBUTTON = 0x00000010,
            POINTER_FLAG_SECONDBUTTON = 0x00000020,
            POINTER_FLAG_THIRDBUTTON = 0x00000040,
            POINTER_FLAG_FOURTHBUTTON = 0x00000080,
            POINTER_FLAG_FIFTHBUTTON = 0x00000100,
            POINTER_FLAG_PRIMARY = 0x00002000,
            POINTER_FLAG_CONFIDENCE = 0x000004000,
            POINTER_FLAG_CANCELED = 0x000008000,
            POINTER_FLAG_DOWN = 0x00010000,
            POINTER_FLAG_UPDATE = 0x00020000,
            POINTER_FLAG_UP = 0x00040000,
            POINTER_FLAG_WHEEL = 0x00080000,
            POINTER_FLAG_HWHEEL = 0x00100000,
            POINTER_FLAG_CAPTURECHANGED = 0x00200000,
            POINTER_FLAG_HASTRANSFORM = 0x00400000,
        }

        internal enum POINTER_BUTTON_CHANGE_TYPE : uint
        {
            POINTER_CHANGE_NONE,
            POINTER_CHANGE_FIRSTBUTTON_DOWN,
            POINTER_CHANGE_FIRSTBUTTON_UP,
            POINTER_CHANGE_SECONDBUTTON_DOWN,
            POINTER_CHANGE_SECONDBUTTON_UP,
            POINTER_CHANGE_THIRDBUTTON_DOWN,
            POINTER_CHANGE_THIRDBUTTON_UP,
            POINTER_CHANGE_FOURTHBUTTON_DOWN,
            POINTER_CHANGE_FOURTHBUTTON_UP,
            POINTER_CHANGE_FIFTHBUTTON_DOWN,
            POINTER_CHANGE_FIFTHBUTTON_UP
        }
        #endregion
    }
}
