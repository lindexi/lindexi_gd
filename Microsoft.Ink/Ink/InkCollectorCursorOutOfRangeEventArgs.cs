// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkCollectorCursorOutOfRangeEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class InkCollectorCursorOutOfRangeEventArgs : EventArgs
  {
    private Cursor m_cursor;

    public Cursor Cursor => this.m_cursor;

    public InkCollectorCursorOutOfRangeEventArgs(Cursor cursor) => this.m_cursor = cursor;
  }
}
