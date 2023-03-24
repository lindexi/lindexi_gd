// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.ITextInputPanelRunInfo
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [TypeLibType(256)]
  [Guid("9F424568-1920-48CC-9811-A993CBF5ADBA")]
  [InterfaceType(1)]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface ITextInputPanelRunInfo
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void IsTipRunning(out int pfRunning);
  }
}
