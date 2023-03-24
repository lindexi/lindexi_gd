// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkTablet3
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
  [DefaultMember("IsMultiTouch")]
  [Guid("7E313997-1327-41DD-8CA9-79F24BE17250")]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface IInkTablet3
  {
    [DispId(0)]
    bool IsMultiTouch { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(1)]
    uint MaximumCursors { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }
  }
}
