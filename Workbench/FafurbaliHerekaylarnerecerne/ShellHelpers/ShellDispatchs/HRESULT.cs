using System.Runtime.InteropServices;

namespace FafurbaliHerekaylarnerecerne;

public readonly record struct HRESULT(int HResult)
{
    public void ThrowIfNotSuccess()
    {
        if (HResult != 0)
        {
            Marshal.ThrowExceptionForHR(HResult);
        }
    }
}