// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.PIManagedInkEventSink
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [Guid("C511ADBE-CD90-4d72-8DC9-96D63E152975")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface PIManagedInkEventSink
  {
    void Invoke(IntPtr pDispatch, int dispid, IntPtr pDisp, IntPtr pVarResult);
  }
}
