// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkWordListClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [DefaultMember("AddWord")]
  [Guid("9DE85094-F71F-44F1-8471-15A2FA76FCF3")]
  [ClassInterface(0)]
  [TypeLibType(2)]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal class InkWordListClass : IInkWordList, InkWordList
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern InkWordListClass();

    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void AddWord([MarshalAs(UnmanagedType.BStr), In] string NewWord);

    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void RemoveWord([MarshalAs(UnmanagedType.BStr), In] string RemoveWord);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Merge([MarshalAs(UnmanagedType.Interface), In] InkWordList MergeWordList);
  }
}
