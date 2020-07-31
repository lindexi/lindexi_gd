using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Xamarin.Designer.Windows.DirectManipulation;

namespace Xamarin.Designer.Windows
{
	class PointerBasedManipulationHandler : IDirectManipulationViewportEventHandler, IDisposable
	{
		// Actual values don't matter
		static tagRECT DefaultViewport = new tagRECT { top = 0, left = 0, right = 1000, bottom = 1000 };

		HwndSource hwndSource;
		IDirectManipulationManager manager;
		IDirectManipulationUpdateManager updateManager;
		IDirectManipulationViewport viewport;
		Size viewportSize;

		uint viewportEventHandlerRegistration;
		float lastScale;

		public PointerBasedManipulationHandler ()
		{
		}

		public event Action<float> ScaleUpdated;

		public HwndSource HwndSource {
			get => hwndSource;
			set {
				var first = hwndSource == null && value != null;
				var oldHwndSource = hwndSource;
				if (oldHwndSource != null)
					oldHwndSource.RemoveHook (WndProcHook);
				if (value != null)
					value.AddHook (WndProcHook);
				this.hwndSource = value;
				//if (first)
				//	InitializeDirectManipulation ();
			}
		}

		IntPtr Window => hwndSource.Handle;

		public void InitializeDirectManipulation (Size size)
		{
			this.manager = (IDirectManipulationManager)Activator.CreateInstance (typeof (DirectManipulationManagerClass));
			var riid = typeof (IDirectManipulationUpdateManager).GUID;
			this.updateManager = manager.GetUpdateManager (ref riid) as IDirectManipulationUpdateManager;
			riid = typeof (IDirectManipulationViewport).GUID;
			this.viewport = manager.CreateViewport (null, Window, ref riid) as IDirectManipulationViewport;

			var configuration = DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_INTERACTION
				| DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_SCALING
				| DIRECTMANIPULATION_CONFIGURATION.DIRECTMANIPULATION_CONFIGURATION_SCALING_INERTIA;
			viewport.ActivateConfiguration (configuration);
			//viewport.SetViewportOptions (DIRECTMANIPULATION_VIEWPORT_OPTIONS.DIRECTMANIPULATION_VIEWPORT_OPTIONS_MANUALUPDATE);
			viewportEventHandlerRegistration = viewport.AddEventHandler (Window, this);
			//viewport.SetViewportRect (ref DefaultViewport);
			SetSize(size, needsStop: false);
			viewport.Enable ();
			manager.Activate(Window);
			//updateManager.Update (null);
		}

		public void Dispose ()
		{
			viewport.RemoveEventHandler (viewportEventHandlerRegistration);
		}

		public void SetSize (Size size, bool needsStop = true)
		{
			if (viewport == null)
				return;
			this.viewportSize = size;
			if (needsStop)
				viewport.Stop ();
			var rect = new tagRECT {
				left = 0,
				top = 0, 
				right = (int)size.Width,
				bottom = (int)size.Height
			};
			viewport.SetViewportRect (ref rect);
		}

		public void Activate() => manager.Activate(Window);

		// Our custom hook to process WM_POINTER event
		IntPtr WndProcHook (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_POINTERDOWN || msg == DM_POINTERHITTEST) {
				var pointerID = GetPointerId (wParam);
				var pointerInfo = default (POINTER_INFO);
				if (!GetPointerInfo (pointerID, ref pointerInfo))
					return IntPtr.Zero;
				if (pointerInfo.pointerType != POINTER_INPUT_TYPE.PT_TOUCHPAD &&
					pointerInfo.pointerType != POINTER_INPUT_TYPE.PT_TOUCH)
					return IntPtr.Zero;

				viewport.SetContact (pointerID);
			} else if (msg == WM_SIZE && manager != null) {
				if (wParam == SIZE_MAXHIDE
					|| wParam == SIZE_MINIMIZED)
					manager.Deactivate (Window);
				/*else
					manager.Activate (Window);*/
			}
			return IntPtr.Zero;
		}

		void ResetViewport (IDirectManipulationViewport viewport)
		{
			viewport.ZoomToRect (0, 0, (float)viewportSize.Width, (float)viewportSize.Height, 0);
			lastScale = 1.0f;
		}

		public void OnViewportStatusChanged ([In, MarshalAs (UnmanagedType.Interface)] IDirectManipulationViewport viewport, [In] DIRECTMANIPULATION_STATUS current, [In] DIRECTMANIPULATION_STATUS previous)
		{
			if (previous == current)
				return;

			if (current == DIRECTMANIPULATION_STATUS.DIRECTMANIPULATION_READY)
				ResetViewport (viewport);
		}

		public void OnViewportUpdated ([In, MarshalAs (UnmanagedType.Interface)] IDirectManipulationViewport viewport)
		{
		}

		public void OnContentUpdated ([In, MarshalAs (UnmanagedType.Interface)] IDirectManipulationViewport viewport, [In, MarshalAs (UnmanagedType.Interface)] IDirectManipulationContent content)
		{
			float[] matrix = new float[6];
			unsafe
			{
				fixed (float* pMatrix = matrix)
					content.GetContentTransform(pMatrix, (uint)matrix.Length);
			}

			float scale = matrix[0];
			if (scale == 0.0f)
				return;

			if (Math.Abs (lastScale - scale) < float.Epsilon)
				return;

			lastScale = scale;
			ScaleUpdated?.Invoke (scale);
		}

		#region Native methods
		const int WM_POINTERDOWN = 0x0246;
		const int DM_POINTERHITTEST = 0x0250;
		const int WM_SIZE = 0x0005;

		static readonly IntPtr SIZE_MAXHIDE = new IntPtr (4);
		static readonly IntPtr SIZE_MINIMIZED = new IntPtr (1);

		/// <summary>
		/// Extracts the pointer id
		/// </summary>
		/// <param name="wParam">The parameter containing the id</param>
		/// <returns>The pointer id</returns>
		uint GetPointerId (IntPtr wParam) => (uint)SignedLOWORD (wParam);
		static int SignedLOWORD (IntPtr intPtr) => SignedLOWORD (IntPtrToInt32 (intPtr));
		static int SignedLOWORD (int n) => (int)(short)(n & 0xFFFF);
		static int IntPtrToInt32 (IntPtr intPtr) => unchecked((int)intPtr.ToInt64 ());

		[DllImport ("user32.dll", EntryPoint = "GetPointerInfo", SetLastError = true)]
		internal static extern bool GetPointerInfo ([In] UInt32 pointerId, [In, Out] ref POINTER_INFO pointerInfo);

		/// <summary>
		/// A structure for holding information related to a pointer.
		/// </summary>
		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct POINTER_INFO
		{
			internal POINTER_INPUT_TYPE pointerType;
			internal UInt32 pointerId;
			internal UInt32 frameId;
			internal POINTER_FLAGS pointerFlags;
			internal IntPtr sourceDevice;
			internal IntPtr hwndTarget;
			internal POINT ptPixelLocation;
			internal POINT ptHimetricLocation;
			internal POINT ptPixelLocationRaw;
			internal POINT ptHimetricLocationRaw;
			internal UInt32 dwTime;
			internal UInt32 historyCount;
			internal Int32 inputData;
			internal UInt32 dwKeyStates;
			internal UInt64 PerformanceCount;
			internal POINTER_BUTTON_CHANGE_TYPE ButtonChangeType;
		}

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct POINT
		{
			internal Int32 X;
			internal Int32 Y;

			public override string ToString ()
			{
				return $"X: {X}, Y: {Y}";
			}
		}

		/// <summary>
		/// The type of input device used (WPF only supports touch and pen)
		/// </summary>
		internal enum POINTER_INPUT_TYPE : UInt32
		{
			PT_POINTER = 0x00000001,
			PT_TOUCH = 0x00000002,
			PT_PEN = 0x00000003,
			PT_MOUSE = 0x00000004,
			PT_TOUCHPAD = 0x00000005
		}

		/// <summary>
		/// Flag field for conveying various pointer info
		/// </summary>
		[Flags]
		internal enum POINTER_FLAGS : UInt32
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

		/// <summary>
		/// State of stylus buttons
		/// </summary>
		internal enum POINTER_BUTTON_CHANGE_TYPE : UInt32
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
