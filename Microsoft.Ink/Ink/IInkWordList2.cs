// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkWordList2
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [Guid("14542586-11BF-4F5F-B6E7-49D0744AAB6E")]
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(4160)]
  [ComImport]
  internal interface IInkWordList2
  {
    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void AddWords([MarshalAs(UnmanagedType.BStr), In] string NewWords);
  }
}
