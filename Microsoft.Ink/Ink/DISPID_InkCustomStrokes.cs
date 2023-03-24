// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkCustomStrokes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkCustomStrokes
  {
    DISPID_ICSs_NewEnum = -4, // 0xFFFFFFFC
    DISPID_ICSsItem = 0,
    DISPID_ICSsCount = 1,
    DISPID_ICSsAdd = 2,
    DISPID_ICSsRemove = 3,
    DISPID_ICSsClear = 4,
  }
}
