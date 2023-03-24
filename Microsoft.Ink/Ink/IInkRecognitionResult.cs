// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkRecognitionResult
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [TypeLibType(4160)]
  [SuppressUnmanagedCodeSecurity]
  [Guid("3BC129A8-86CD-45AD-BDE8-E0D32D61C16D")]
  [ComImport]
  internal interface IInkRecognitionResult
  {
    [DispId(1)]
    string TopString { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(2)]
    IInkRecognitionAlternate TopAlternate { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(4)]
    InkRecognitionConfidence TopConfidence { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(3)]
    InkStrokes Strokes { [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(5)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    IInkRecognitionAlternates AlternatesFromSelection(
      [In] int selectionStart = 0,
      [In] int selectionLength = -1,
      [In] int maximumAlternates = 10);

    [DispId(6)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ModifyTopAlternate([MarshalAs(UnmanagedType.Interface), In] IInkRecognitionAlternate Alternate);

    [DispId(7)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetResultOnStrokes();
  }
}
