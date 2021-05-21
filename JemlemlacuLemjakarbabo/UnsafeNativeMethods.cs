using System;
using System.Runtime.InteropServices;
using System.Text;

namespace JemlemlacuLemjakarbabo
{
    internal static partial class UnsafeNativeMethods
    {
        internal static class WICCodec
        {
            // When updating this value be sure to update milrender.h's version
            // Reserve the number by editing and resubmitting SDKVersion.txt
            internal const int WINCODEC_SDK_VERSION = 0x0236;

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICCreateImagingFactory_Proxy")]
            internal static extern int CreateImagingFactory(
                UInt32 SDKVersion,
                out IntPtr ppICodecFactory
            );

            //[DllImport(DllImport.WindowsCodecs, EntryPoint = "WICConvertBitmapSource")]
            //internal static extern int /* HRESULT */ WICConvertBitmapSource(
            //    ref Guid dstPixelFormatGuid,
            //    SafeMILHandle /* IWICBitmapSource */ pISrc,
            //    out BitmapSourceSafeMILHandle /* IWICBitmapSource* */ ppIDst);

            //[DllImport(DllImport.WindowsCodecs, EntryPoint = "WICSetEncoderFormat_Proxy")]
            //internal static extern int /* HRESULT */ WICSetEncoderFormat(
            //    SafeMILHandle /* IWICBitmapSource */ pSourceIn,
            //    SafeMILHandle /* IMILPalette */ pIPalette,
            //    SafeMILHandle /* IWICBitmapFrameEncode*  */ pIFrameEncode,
            //    out SafeMILHandle /* IWICBitmapSource**  */ ppSourceOut);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICMapGuidToShortName")]//CASRemoval:
            internal static extern int /* HRESULT */ WICMapGuidToShortName(
                ref Guid guid,
                uint cchName,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzName,
                ref uint pcchActual);

            [DllImport(DllImport.WindowsCodecs, EntryPoint = "WICMapShortNameToGuid")]//CASRemoval:
            internal static extern int /* HRESULT */ WICMapShortNameToGuid(
                [MarshalAs(UnmanagedType.LPWStr)] String wzName,
                ref Guid guid);

            //[DllImport(DllImport.WindowsCodecsExt, EntryPoint = "WICCreateColorTransform_Proxy")]
            //internal static extern int /* HRESULT */ CreateColorTransform(
            //    out BitmapSourceSafeMILHandle  /* IWICColorTransform */ ppWICColorTransform);

            //[DllImport(DllImport.WindowsCodecs, EntryPoint = "WICCreateColorContext_Proxy")]
            //internal static extern int /* HRESULT */ CreateColorContext(
            //    IntPtr pICodecFactory,
            //    out System.Windows.Media.SafeMILHandle /* IWICColorContext */ ppColorContext);

            [DllImport("ole32.dll")]
            internal static extern int /* HRESULT */ CoInitialize(
                IntPtr reserved);

            [DllImport("ole32.dll")]
            internal static extern void CoUninitialize();
        }

        internal static class WICImagingFactory
        {
            [DllImport(DllImport.WindowsCodecs, EntryPoint = "IWICImagingFactory_CreateDecoderFromFileHandle_Proxy")]
            internal static extern int /*HRESULT*/ CreateDecoderFromFileHandle(
                IntPtr pICodecFactory,
                Microsoft.Win32.SafeHandles.SafeFileHandle  /*ULONG_PTR*/ hFileHandle,
                ref Guid guidVendor,
                UInt32 metadataFlags,
                out IntPtr /* IWICBitmapDecoder */ ppIDecode);
        }

    }
}