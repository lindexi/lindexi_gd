// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkRecognizer2
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [TypeLibType(4160)]
  [Guid("6110118A-3A75-4AD6-B2AA-04B2B72BBE65")]
  [DefaultMember("Id")]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface IInkRecognizer2
  {
    [DispId(0)]
    string Id { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(1)]
    object UnicodeRanges { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Struct)] get; }
  }
}
