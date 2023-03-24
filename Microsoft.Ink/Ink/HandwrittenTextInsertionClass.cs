// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.HandwrittenTextInsertionClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [ClassInterface(0)]
  [Guid("9F074EE2-E6E9-4D8A-A047-EB5B5C3C55DA")]
  [TypeLibType(2)]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal class HandwrittenTextInsertionClass : 
    IHandwrittenTextInsertion,
    HandwrittenTextInsertionPrivate
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern HandwrittenTextInsertionClass();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void InsertRecognitionResultsArray(
      [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR), In] Array psaAlternates,
      [In] uint locale,
      [In] int fAlternateContainsAutoSpacingInformation);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void InsertInkRecognitionResult(
      [MarshalAs(UnmanagedType.Interface), In] IInkRecognitionResult pIInkRecoResult,
      [In] uint locale,
      [In] int fAlternateContainsAutoSpacingInformation);
  }
}
