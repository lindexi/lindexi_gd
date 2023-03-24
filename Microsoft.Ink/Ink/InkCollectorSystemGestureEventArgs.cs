// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkCollectorSystemGestureEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class InkCollectorSystemGestureEventArgs : EventArgs
  {
    private Cursor m_cursor;
    private Point m_point;
    private SystemGesture m_id;
    private int m_modifier;
    private char m_character;
    private int m_cursormode;

    public Cursor Cursor => this.m_cursor;

    public Point Point => this.m_point;

    public SystemGesture Id => this.m_id;

    public int Modifier => this.m_modifier;

    public char Character => this.m_character;

    public int CursorMode => this.m_cursormode;

    public InkCollectorSystemGestureEventArgs(
      Cursor cursor,
      SystemGesture id,
      Point pt,
      int modifier,
      char c,
      int mode)
    {
      this.m_cursor = cursor;
      this.m_modifier = modifier;
      this.m_id = id;
      this.m_point = pt;
      this.m_character = c;
      this.m_cursormode = mode;
    }
  }
}
