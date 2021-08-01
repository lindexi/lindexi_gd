using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace JemlemlacuLemjakarbabo
{
    class Program
    {
        static void Main(string[] args)
        {
            CheckHResult(UnsafeNativeMethods.WICCodec.CreateImagingFactory(UnsafeNativeMethods.WICCodec.WINCODEC_SDK_VERSION,
                out var pImagingFactory));

            using var fs = new FileStream("image.jpg",
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous);
            var stream = fs;
            SafeFileHandle safeFilehandle

            System.IO.FileStream filestream = stream as System.IO.FileStream;
            try
            {
                if (filestream.IsAsync is false)
                {
                    safeFilehandle = filestream.SafeFileHandle;
                }
                else
                {
                    // If Filestream is async that doesn't support IWICImagingFactory_CreateDecoderFromFileHandle_Proxy, then revert to old code path.
                    safeFilehandle = null;
                }
            }
            catch
            {
                // If Filestream doesn't support SafeHandle then revert to old code path.
                // See https://github.com/dotnet/wpf/issues/4355
                safeFilehandle = null;
            }

            Guid vendorMicrosoft = new Guid(MILGuidData.GUID_VendorMicrosoft);
            UInt32 metadataFlags = (uint)WICMetadataCacheOptions.WICMetadataCacheOnDemand;

            CheckHResult
            (
                UnsafeNativeMethods.WICImagingFactory.CreateDecoderFromFileHandle
                (
                    pImagingFactory,
                    fs.SafeFileHandle,
                    ref vendorMicrosoft,
                    metadataFlags,
                    out var decoder
                )
            );
        }

        static void CheckHResult(int hr)
        {
            if (hr < 0)
            {
                Exception exceptionForHR = Marshal.GetExceptionForHR(hr, (IntPtr)(-1));

                throw exceptionForHR;
            }
        }
    }
}
