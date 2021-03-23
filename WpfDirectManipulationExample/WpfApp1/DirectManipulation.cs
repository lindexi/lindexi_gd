using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
namespace Xamarin.Designer.Windows.DirectManipulation
{
	[Guid ("FBF5D3B4-70C7-4163-9322-5A6F660D6FBC")]
	[CoClass (typeof (DirectManipulationManagerClass))]
	[ComImport]
	public interface DirectManipulationManager : IDirectManipulationManager
	{
	}

	[Guid ("54E211B6-3650-4F75-8334-FA359598E1C5")]
	[TypeLibType (TypeLibTypeFlags.FCanCreate)]
	[ClassInterface (ClassInterfaceType.None)]
	[ComImport]
	public class DirectManipulationManagerClass
	{
	}


	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[SuppressUnmanagedCodeSecurity]
	[Guid ("FBF5D3B4-70C7-4163-9322-5A6F660D6FBC")]
	[ComImport]
	public interface IDirectManipulationManager
	{
		[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Activate ([In] IntPtr window);

		[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Deactivate ([In] IntPtr window);

		[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void RegisterHitTestTarget ([In] IntPtr window, [In] IntPtr hitTestWindow, [In] DIRECTMANIPULATION_HITTEST_TYPE type);

		[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		int ProcessInput ([In] ref tagMSG message);

		[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[return: MarshalAs (UnmanagedType.IUnknown)]
		object GetUpdateManager ([In] ref Guid riid);

		[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[return: MarshalAs (UnmanagedType.IUnknown)]
		object CreateViewport ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationFrameInfoProvider frameInfo, [In] IntPtr window, [In] ref Guid riid);

		[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[return: MarshalAs (UnmanagedType.IUnknown)]
		object CreateContent ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationFrameInfoProvider frameInfo, [In] ref Guid clsid, [In] ref Guid riid);
	}

	[Guid ("B89962CB-3D89-442B-BB58-5098FA0F9F16")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public unsafe interface IDirectManipulationContent
	{
		void GetContentRect (out tagRECT contentSize);

		void SetContentRect ([In] ref tagRECT contentSize);

		IntPtr GetViewport ([In] ref Guid riid);

		void GetTag ([In] ref Guid riid, out IntPtr @object, out uint id);

		void SetTag ([MarshalAs (UnmanagedType.IUnknown), In] object @object, [In] uint id);

		void GetOutputTransform (float* matrix, [In] uint pointCount);

		void GetContentTransform (float* matrix, [In] uint pointCount);

		void SyncContentTransform ([In] ref float matrix, [In] uint pointCount);
	}

	[Guid ("FB759DBA-6F4C-4C01-874E-19C8A05907F9")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IDirectManipulationFrameInfoProvider
	{
		void GetNextFrameInfo (out ulong time, out ulong processTime, out ulong compositionTime);
	}


	[Guid ("C12851E4-1698-4625-B9B1-7CA3EC18630B")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IDirectManipulationPrimaryContent
	{
		void SetSnapInterval ([In] DIRECTMANIPULATION_MOTION_TYPES motion, [In] float interval, [In] float offset);

		void SetSnapPoints ([In] DIRECTMANIPULATION_MOTION_TYPES motion, [In] ref float points, [In] uint pointCount);

		void SetSnapType ([In] DIRECTMANIPULATION_MOTION_TYPES motion, [In] DIRECTMANIPULATION_SNAPPOINT_TYPE type);

		void SetSnapCoordinate ([In] DIRECTMANIPULATION_MOTION_TYPES motion, [In] DIRECTMANIPULATION_SNAPPOINT_COORDINATE coordinate, [In] float origin);

		void SetZoomBoundaries ([In] float zoomMinimum, [In] float zoomMaximum);

		void SetHorizontalAlignment ([In] DIRECTMANIPULATION_HORIZONTALALIGNMENT alignment);

		void SetVerticalAlignment ([In] DIRECTMANIPULATION_VERTICALALIGNMENT alignment);

		void GetInertiaEndTransform (out float matrix, [In] uint pointCount);

		void GetCenterPoint (out float centerX, out float centerY);
	}

	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[Guid ("790B6337-64F8-4FF5-A269-B32BC2AF27A7")]
	[ComImport]
	public interface IDirectManipulationUpdateHandler
	{
		void Update ();
	}

	[Guid ("B0AE62FD-BE34-46E7-9CAA-D361FACBB9CC")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IDirectManipulationUpdateManager
	{
		void RegisterWaitHandleCallback ([In] IntPtr handle, [MarshalAs (UnmanagedType.Interface), In] IDirectManipulationUpdateHandler eventHandler, out uint cookie);

		void UnregisterWaitHandleCallback ([In] uint cookie);

		void Update ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationFrameInfoProvider frameInfo);
	}

	[Guid ("28B85A3D-60A0-48BD-9BA1-5CE8D9EA3A6D")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IDirectManipulationViewport
	{
		void Enable ();

		void Disable ();

		void SetContact ([In] uint pointerId);

		void ReleaseContact ([In] uint pointerId);

		void ReleaseAllContacts ();

		DIRECTMANIPULATION_STATUS GetStatus ();

		void GetTag ([In] ref Guid riid, out IntPtr @object, out uint id);

		void SetTag ([MarshalAs (UnmanagedType.IUnknown), In] object @object, [In] uint id);

		tagRECT GetViewportRect ();

		void SetViewportRect ([In] ref tagRECT viewport);

		void ZoomToRect ([In] float left, [In] float top, [In] float right, [In] float bottom, [In] int animate);

		void SetViewportTransform ([In] ref float matrix, [In] uint pointCount);

		void SyncDisplayTransform ([In] ref float matrix, [In] uint pointCount);

		IntPtr GetPrimaryContent ([In] ref Guid riid);

		void AddContent ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationContent content);

		void RemoveContent ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationContent content);

		void SetViewportOptions ([In] DIRECTMANIPULATION_VIEWPORT_OPTIONS options);

		void AddConfiguration ([In] DIRECTMANIPULATION_CONFIGURATION configuration);

		void RemoveConfiguration ([In] DIRECTMANIPULATION_CONFIGURATION configuration);

		void ActivateConfiguration ([In] DIRECTMANIPULATION_CONFIGURATION configuration);

		void SetManualGesture ([In] DIRECTMANIPULATION_GESTURE_CONFIGURATION configuration);

		void SetChaining ([In] DIRECTMANIPULATION_MOTION_TYPES enabledTypes);

		uint AddEventHandler ([In] IntPtr window, [MarshalAs (UnmanagedType.Interface), In] IDirectManipulationViewportEventHandler eventHandler);

		void RemoveEventHandler ([In] uint cookie);

		void SetInputMode ([In] DIRECTMANIPULATION_INPUT_MODE mode);

		void SetUpdateMode ([In] DIRECTMANIPULATION_INPUT_MODE mode);

		void Stop ();

		void Abandon ();
	}

	[Guid ("923CCAAC-61E1-4385-B726-017AF189882A")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IDirectManipulationViewport2 : IDirectManipulationViewport
	{
		new void Enable ();

		new void Disable ();

		new void SetContact ([In] uint pointerId);

		new void ReleaseContact ([In] uint pointerId);

		new void ReleaseAllContacts ();

		new DIRECTMANIPULATION_STATUS GetStatus ();

		new void GetTag ([In] ref Guid riid, out IntPtr @object, out uint id);

		new void SetTag ([MarshalAs (UnmanagedType.IUnknown), In] object @object, [In] uint id);

		new tagRECT GetViewportRect ();

		new void SetViewportRect ([In] ref tagRECT viewport);

		new void ZoomToRect ([In] float left, [In] float top, [In] float right, [In] float bottom, [In] int animate);

		new void SetViewportTransform ([In] ref float matrix, [In] uint pointCount);

		new void SyncDisplayTransform ([In] ref float matrix, [In] uint pointCount);

		new IntPtr GetPrimaryContent ([In] ref Guid riid);

		new void AddContent ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationContent content);

		new void RemoveContent ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationContent content);

		new void SetViewportOptions ([In] DIRECTMANIPULATION_VIEWPORT_OPTIONS options);

		new void AddConfiguration ([In] DIRECTMANIPULATION_CONFIGURATION configuration);

		new void RemoveConfiguration ([In] DIRECTMANIPULATION_CONFIGURATION configuration);

		new void ActivateConfiguration ([In] DIRECTMANIPULATION_CONFIGURATION configuration);

		new void SetManualGesture ([In] DIRECTMANIPULATION_GESTURE_CONFIGURATION configuration);

		new void SetChaining ([In] DIRECTMANIPULATION_MOTION_TYPES enabledTypes);

		new uint AddEventHandler ([In] IntPtr window, [MarshalAs (UnmanagedType.Interface), In] IDirectManipulationViewportEventHandler eventHandler);

		new void RemoveEventHandler ([In] uint cookie);

		new void SetInputMode ([In] DIRECTMANIPULATION_INPUT_MODE mode);

		new void SetUpdateMode ([In] DIRECTMANIPULATION_INPUT_MODE mode);

		new void Stop ();

		new void Abandon ();

		uint AddBehavior ([MarshalAs (UnmanagedType.IUnknown), In] object behavior);

		void RemoveBehavior ([In] uint cookie);

		void RemoveAllBehaviors ();
	}

	[Guid ("952121DA-D69F-45F9-B0F9-F23944321A6D")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IDirectManipulationViewportEventHandler
	{
		void OnViewportStatusChanged ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationViewport viewport, [In] DIRECTMANIPULATION_STATUS current, [In] DIRECTMANIPULATION_STATUS previous);

		void OnViewportUpdated ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationViewport viewport);

		void OnContentUpdated ([MarshalAs (UnmanagedType.Interface), In] IDirectManipulationViewport viewport, [MarshalAs (UnmanagedType.Interface), In] IDirectManipulationContent content);
	}

	[StructLayout (LayoutKind.Sequential, Pack = 4)]
	public struct tagMSG
	{
		[ComAliasName ("DirectManipulation.wireHWND")]
		public IntPtr hwnd;
		public uint message;
		[ComAliasName ("DirectManipulation.UINT_PTR")]
		public uint wParam;
		[ComAliasName ("DirectManipulation.LONG_PTR")]
		public int lParam;
		public uint time;
		public tagPOINT pt;
	}

	[StructLayout (LayoutKind.Sequential, Pack = 4)]
	public struct tagPOINT
	{
		public int x;
		public int y;
	}

	[StructLayout (LayoutKind.Sequential, Pack = 4)]
	public struct tagRECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	public enum DIRECTMANIPULATION_CONFIGURATION
	{
		DIRECTMANIPULATION_CONFIGURATION_NONE = 0,
		DIRECTMANIPULATION_CONFIGURATION_INTERACTION = 1,
		DIRECTMANIPULATION_CONFIGURATION_TRANSLATION_X = 2,
		DIRECTMANIPULATION_CONFIGURATION_TRANSLATION_Y = 4,
		DIRECTMANIPULATION_CONFIGURATION_SCALING = 16, // 0x00000010
		DIRECTMANIPULATION_CONFIGURATION_TRANSLATION_INERTIA = 32, // 0x00000020
		DIRECTMANIPULATION_CONFIGURATION_SCALING_INERTIA = 128, // 0x00000080
		DIRECTMANIPULATION_CONFIGURATION_RAILS_X = 256, // 0x00000100
		DIRECTMANIPULATION_CONFIGURATION_RAILS_Y = 512, // 0x00000200
	}

	public enum DIRECTMANIPULATION_GESTURE_CONFIGURATION
	{
		DIRECTMANIPULATION_GESTURE_DEFAULT = 0,
		DIRECTMANIPULATION_GESTURE_NONE = 0,
		DIRECTMANIPULATION_GESTURE_CROSS_SLIDE_VERTICAL = 8,
		DIRECTMANIPULATION_GESTURE_CROSS_SLIDE_HORIZONTAL = 16, // 0x00000010
		DIRECTMANIPULATION_GESTURE_PINCH_ZOOM = 32, // 0x00000020
	}

	public enum DIRECTMANIPULATION_HITTEST_TYPE
	{
		DIRECTMANIPULATION_HITTEST_TYPE_ASYNCHRONOUS,
		DIRECTMANIPULATION_HITTEST_TYPE_SYNCHRONOUS,
		DIRECTMANIPULATION_HITTEST_TYPE_AUTO_SYNCHRONOUS,
	}

	public enum DIRECTMANIPULATION_HORIZONTALALIGNMENT
	{
		DIRECTMANIPULATION_HORIZONTALALIGNMENT_NONE = 0,
		DIRECTMANIPULATION_HORIZONTALALIGNMENT_LEFT = 1,
		DIRECTMANIPULATION_HORIZONTALALIGNMENT_CENTER = 2,
		DIRECTMANIPULATION_HORIZONTALALIGNMENT_RIGHT = 4,
		DIRECTMANIPULATION_HORIZONTALALIGNMENT_UNLOCKCENTER = 8,
	}

	public enum DIRECTMANIPULATION_INPUT_MODE
	{
		DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC,
		DIRECTMANIPULATION_INPUT_MODE_MANUAL,
	}

	public enum DIRECTMANIPULATION_MOTION_TYPES
	{
		DIRECTMANIPULATION_MOTION_NONE = 0,
		DIRECTMANIPULATION_MOTION_TRANSLATEX = 1,
		DIRECTMANIPULATION_MOTION_TRANSLATEY = 2,
		DIRECTMANIPULATION_MOTION_ZOOM = 4,
		DIRECTMANIPULATION_MOTION_CENTERX = 16, // 0x00000010
		DIRECTMANIPULATION_MOTION_CENTERY = 32, // 0x00000020
		DIRECTMANIPULATION_MOTION_ALL = 55, // 0x00000037
	}

	public enum DIRECTMANIPULATION_SNAPPOINT_COORDINATE
	{
		DIRECTMANIPULATION_COORDINATE_BOUNDARY = 0,
		DIRECTMANIPULATION_COORDINATE_ORIGIN = 1,
		DIRECTMANIPULATION_COORDINATE_MIRRORED = 16, // 0x00000010
	}

	public enum DIRECTMANIPULATION_SNAPPOINT_TYPE
	{
		DIRECTMANIPULATION_SNAPPOINT_MANDATORY,
		DIRECTMANIPULATION_SNAPPOINT_OPTIONAL,
		DIRECTMANIPULATION_SNAPPOINT_MANDATORY_SINGLE,
		DIRECTMANIPULATION_SNAPPOINT_OPTIONAL_SINGLE,
	}

	public enum DIRECTMANIPULATION_STATUS
	{
		DIRECTMANIPULATION_BUILDING,
		DIRECTMANIPULATION_ENABLED,
		DIRECTMANIPULATION_DISABLED,
		DIRECTMANIPULATION_RUNNING,
		DIRECTMANIPULATION_INERTIA,
		DIRECTMANIPULATION_READY,
		DIRECTMANIPULATION_SUSPENDED,
	}

	public enum DIRECTMANIPULATION_VERTICALALIGNMENT
	{
		DIRECTMANIPULATION_VERTICALALIGNMENT_NONE = 0,
		DIRECTMANIPULATION_VERTICALALIGNMENT_TOP = 1,
		DIRECTMANIPULATION_VERTICALALIGNMENT_CENTER = 2,
		DIRECTMANIPULATION_VERTICALALIGNMENT_BOTTOM = 4,
		DIRECTMANIPULATION_VERTICALALIGNMENT_UNLOCKCENTER = 8,
	}

	public enum DIRECTMANIPULATION_VIEWPORT_OPTIONS
	{
		DIRECTMANIPULATION_VIEWPORT_OPTIONS_DEFAULT = 0,
		DIRECTMANIPULATION_VIEWPORT_OPTIONS_AUTODISABLE = 1,
		DIRECTMANIPULATION_VIEWPORT_OPTIONS_MANUALUPDATE = 2,
		DIRECTMANIPULATION_VIEWPORT_OPTIONS_INPUT = 4,
		DIRECTMANIPULATION_VIEWPORT_OPTIONS_EXPLICITHITTEST = 8,
		DIRECTMANIPULATION_VIEWPORT_OPTIONS_DISABLEPIXELSNAPPING = 16, // 0x00000010
	}
}
