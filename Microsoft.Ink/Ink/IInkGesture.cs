// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkGesture
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [DefaultMember("Id")]
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(4160)]
  [Guid("3BDC0A97-04E5-4E26-B813-18F052D41DEF")]
  [ComImport]
  internal interface IInkGesture
  {
    [DispId(2)]
    InkRecognitionConfidence Confidence { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(0)]
    InkApplicationGesture Id { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetHotPoint([In, Out] ref int x, [In, Out] ref int y);
  }
}
