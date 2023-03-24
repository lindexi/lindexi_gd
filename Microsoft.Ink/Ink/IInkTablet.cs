// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkTablet
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [Guid("2DE25EAA-6EF8-42D5-AEE9-185BC81B912D")]
  [DefaultMember("Name")]
  [TypeLibType(4160)]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface IInkTablet
  {
    [DispId(0)]
    string Name { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(1)]
    string PlugAndPlayId { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(4)]
    InkRectangle MaximumInputRectangle { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(5)]
    TabletHardwareCapabilitiesPrivate HardwareCapabilities { [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    bool IsPacketPropertySupported([MarshalAs(UnmanagedType.BStr), In] string packetPropertyName);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetPropertyMetrics(
      [MarshalAs(UnmanagedType.BStr), In] string propertyName,
      out int Minimum,
      out int Maximum,
      out TabletPropertyMetricUnitPrivate Units,
      out float Resolution);
  }
}
