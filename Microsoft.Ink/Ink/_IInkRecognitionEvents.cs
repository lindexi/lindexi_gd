// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkRecognitionEvents
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [TypeLibType(4096)]
  [Guid("17BCE92F-2E21-47FD-9D33-3C6AFBFD8C59")]
  [InterfaceType(2)]
  [ComImport]
  internal interface _IInkRecognitionEvents
  {
    [DispId(1)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RecognitionWithAlternates(
      [MarshalAs(UnmanagedType.Interface), In] IInkRecognitionResult RecognitionResult,
      [MarshalAs(UnmanagedType.Struct), In] object CustomData,
      [In] InkRecognitionStatusPrivate RecognitionStatus);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Recognition(
      [MarshalAs(UnmanagedType.BStr), In] string RecognizedString,
      [MarshalAs(UnmanagedType.Struct), In] object CustomData,
      [In] InkRecognitionStatusPrivate RecognitionStatus);
  }
}
