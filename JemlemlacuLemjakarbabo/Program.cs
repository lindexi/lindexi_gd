using System;
using System.IO;
using System.Runtime.InteropServices;

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
