// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.ComObjectCreator
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;

namespace Microsoft.StylusInput
{
  internal static class ComObjectCreator
  {
    public static object CreateInstanceLicense(Guid clsid, Guid iid, string licenseKey)
    {
      object ppvObj = (object) null;
      Guid guid = typeof (UnsafeNativeMethods.IClassFactory2).GUID;
      UnsafeNativeMethods.CoGetClassObject(ref clsid, 1U, IntPtr.Zero, ref guid).CreateInstanceLic(IntPtr.Zero, IntPtr.Zero, ref iid, licenseKey, out ppvObj);
      return ppvObj;
    }
  }
}
