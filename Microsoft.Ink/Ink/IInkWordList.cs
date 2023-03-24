// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkWordList
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [Guid("76BA3491-CB2F-406B-9961-0E0C4CDAAEF2")]
  [DefaultMember("AddWord")]
  [TypeLibType(4160)]
  [ComImport]
  internal interface IInkWordList
  {
    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void AddWord([MarshalAs(UnmanagedType.BStr), In] string NewWord);

    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoveWord([MarshalAs(UnmanagedType.BStr), In] string RemoveWord);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Merge([MarshalAs(UnmanagedType.Interface), In] InkWordList MergeWordList);
  }
}
