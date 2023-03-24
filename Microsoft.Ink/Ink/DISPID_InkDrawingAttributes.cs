// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkDrawingAttributes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkDrawingAttributes
  {
    DISPID_DAHeight = 1,
    DISPID_DAColor = 2,
    DISPID_DAWidth = 3,
    DISPID_DAFitToCurve = 4,
    DISPID_DAIgnorePressure = 5,
    DISPID_DAAntiAliased = 6,
    DISPID_DATransparency = 7,
    DISPID_DARasterOperation = 8,
    DISPID_DAPenTip = 9,
    DISPID_DAClone = 10, // 0x0000000A
    DISPID_DAExtendedProperties = 11, // 0x0000000B
  }
}
