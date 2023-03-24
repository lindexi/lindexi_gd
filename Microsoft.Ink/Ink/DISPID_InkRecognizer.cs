// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkRecognizer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkRecognizer
  {
    DISPID_RecoClsid = 1,
    DISPID_RecoName = 2,
    DISPID_RecoVendor = 3,
    DISPID_RecoCapabilities = 4,
    DISPID_RecoLanguageID = 5,
    DISPID_RecoPreferredPacketDescription = 6,
    DISPID_RecoCreateRecognizerContext = 7,
    DISPID_RecoSupportedProperties = 8,
  }
}
