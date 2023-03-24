// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.CorrectionMode
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [Guid("D708F745-981E-4E9B-AFA0-98082AADB421")]
  internal enum CorrectionMode
  {
    CorrectionMode_NotVisible,
    CorrectionMode_PreInsertion,
    CorrectionMode_PostInsertionCollapsed,
    CorrectionMode_PostInsertionExpanded,
  }
}
