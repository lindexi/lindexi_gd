// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InteractionMode
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [Guid("500F9C5A-6739-449B-9CFA-5FC2F2E9DDCE")]
  internal enum InteractionMode
  {
    InteractionMode_InPlace,
    InteractionMode_Floating,
    InteractionMode_DockedTop,
    InteractionMode_DockedBottom,
  }
}
