// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.__MIDL_IWinTypes_0009
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [StructLayout(LayoutKind.Explicit, Pack = 4)]
  internal struct __MIDL_IWinTypes_0009
  {
    [FieldOffset(0)]
    public int hInproc;
    [FieldOffset(0)]
    public int hRemote;
  }
}
