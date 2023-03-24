// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkTablet2
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
  [DefaultMember("DeviceKind")]
  [Guid("90C91AD2-FA36-49D6-9516-CE8D570F6F85")]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface IInkTablet2
  {
    [DispId(0)]
    TabletDeviceKindPrivate DeviceKind { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }
  }
}
