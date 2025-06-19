using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Boost;

partial class Win32
{
    /// <summary>
    /// <para>
    /// The <see cref="GpStatus"/> enumeration indicates the result of a Windows GDI+ method call.
    /// </para>
    /// <para>
    /// From: https://docs.microsoft.com/zh-cn/windows/win32/api/gdiplustypes/ne-gdiplustypes-status
    /// </para>
    /// </summary>
    public enum GpStatus
    {
        /// <summary>
        /// Indicates that the method call was successful.
        /// </summary>
        Ok,

        /// <summary>
        /// Indicates that there was an error on the method call, which is identified as something other than those defined by the other elements of this enumeration.
        /// </summary>
        GenericError,

        /// <summary>
        /// Indicates that one of the arguments passed to the method was not valid.
        /// </summary>
        InvalidParameter,

        /// <summary>
        /// Indicates that the operating system is out of memory and could not allocate memory to process the method call.
        /// For an explanation of how constructors use the OutOfMemory status, see the Remarks section at the end of this topic.
        /// </summary>
        OutOfMemory,

        /// <summary>
        /// Indicates that one of the arguments specified in the API call is already in use in another thread.
        /// </summary>
        ObjectBusy,

        /// <summary>
        /// Indicates that a buffer specified as an argument in the API call is not large enough to hold the data to be received.
        /// </summary>
        InsufficientBuffer,

        /// <summary>
        /// Indicates that the method is not implemented.
        /// </summary>
        NotImplemented,

        /// <summary>
        /// Indicates that the method generated a Win32 error.
        /// </summary>
        Win32Error,

        /// <summary>
        /// Indicates that the object is in an invalid state to satisfy the API call.
        /// For example, calling Pen::GetColor from a pen that is not a single, solid color results in a WrongState status.
        /// </summary>
        WrongState,

        /// <summary>
        /// Indicates that the method was aborted.
        /// </summary>
        Aborted,

        /// <summary>
        /// Indicates that the specified image file or metafile cannot be found.
        /// </summary>
        FileNotFound,

        /// <summary>
        /// Indicates that the method performed an arithmetic operation that produced a numeric overflow.
        /// </summary>
        ValueOverflow,

        /// <summary>
        /// Indicates that a write operation is not allowed on the specified file.
        /// </summary>
        AccessDenied,

        /// <summary>
        /// Indicates that the specified image file format is not known.
        /// </summary>
        UnknownImageFormat,

        /// <summary>
        /// Indicates that the specified font family cannot be found. Either the font family name is incorrect or the font family is not installed.
        /// </summary>
        FontFamilyNotFound,

        /// <summary>
        /// Indicates that the specified style is not available for the specified font family.
        /// </summary>
        FontStyleNotFound,

        /// <summary>
        /// Indicates that the font retrieved from an HDC or LOGFONT is not a TrueType font and cannot be used with GDI+.
        /// </summary>
        NotTrueTypeFont,

        /// <summary>
        /// Indicates that the version of GDI+ that is installed on the system is incompatible with the version with which the application was compiled.
        /// </summary>
        UnsupportedGdiplusVersion,

        /// <summary>
        /// Indicates that the GDI+API is not in an initialized state.
        /// To function, all GDI+ objects require that GDI+ be in an initialized state.
        /// Initialize GDI+ by calling GdiplusStartup.
        /// </summary>
        GdiplusNotInitialized,

        /// <summary>
        /// Indicates that the specified property does not exist in the image.
        /// </summary>
        PropertyNotFound,

        /// <summary>
        /// Indicates that the specified property is not supported by the format of the image and, therefore, cannot be set.
        /// </summary>
        PropertyNotSupported,

        /// <summary>
        /// Indicates that the color profile required to save an image in CMYK format was not found.
        /// </summary>
        ProfileNotFound,
    }

    /// <summary>
    /// <para>
    /// The <see cref="GdiplusStartupInput"/> structure holds a block of arguments that are required by the <see cref="GdiplusStartup"/> function.
    /// </para>
    /// <para>
    /// From: https://docs.microsoft.com/zh-cn/windows/win32/api/gdiplusinit/ns-gdiplusinit-gdiplusstartupinput
    /// </para>
    /// </summary>
    public struct GdiplusStartupInput
    {
        /// <summary>
        /// Specifies the version of GDI+. Must be 1.
        /// </summary>
        public uint GdiplusVersion;

        /// <summary>
        /// Pointer to a callback function that GDI+ can call, on debug builds, for assertions and warnings. The default value is NULL.
        /// </summary>
        public IntPtr DebugEventCallback;

        /// <summary>
        /// Boolean value that specifies whether to suppress the GDI+ background thread. 
        /// If you set this member to TRUE, <see cref="GdiplusStartup"/> returns (in its output parameter) a pointer to a hook function and a pointer to an unhook function.
        /// You must call those functions appropriately to replace the background thread.
        /// If you do not want to be responsible for calling the hook and unhook functions, set this member to FALSE. The default value is FALSE.
        /// </summary>
        public bool SuppressBackgroundThread;

        /// <summary>
        /// Boolean value that specifies whether you want GDI+ to suppress external image codecs.
        /// GDI+ version 1.0 does not support external image codecs, so this parameter is ignored.
        /// </summary>
        public bool SuppressExternalCodecs;
    }

    /// <summary>
    /// <para>
    /// The <see cref="GdiplusStartup"/> function uses the <see cref="GdiplusStartupOutput"/> structure to return (in its output parameter) a pointer to
    /// a hook function and a pointer to an unhook function. If you set the <see cref="GdiplusStartupInput.SuppressBackgroundThread"/> member of the input parameter to TRUE,
    /// then you are responsible for calling those functions to replace the Windows GDI+ background thread.
    /// Call the hook and unhook functions before and after the application's main message loop; that is, a message loop that is active for the lifetime of GDI+.
    /// Call the hook function before the loop starts, and call the unhook function after the loop ends. The token parameter of the hook function receives an identifier
    /// that you should later pass to the unhook function. If you do not pass the proper identifier (the one returned by the hook function) to the unhook function,
    /// there will be resource leaks that won't be cleaned up until the process exits.
    /// If you do not want to be responsible for calling the hook and unhook functions, set the <see cref="GdiplusStartupInput.SuppressBackgroundThread"/> member
    /// of the input parameter (passed to <see cref="GdiplusStartup"/>) to FALSE.
    /// </para>
    /// <para>
    /// From: https://docs.microsoft.com/zh-cn/windows/win32/api/gdiplusinit/ns-gdiplusinit-gdiplusstartupoutput
    /// </para>
    /// </summary>
    public struct GdiplusStartupOutput
    {
        /// <summary>
        /// Receives a pointer to a hook function.
        /// </summary>
        IntPtr NotificationHook;

        /// <summary>
        /// Receives a pointer to an unhook function.
        /// </summary>
        IntPtr NotificationUnhook;
    }
}
