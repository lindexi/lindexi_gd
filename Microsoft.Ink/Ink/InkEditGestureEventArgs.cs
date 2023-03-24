// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkEditGestureEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.ComponentModel;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class InkEditGestureEventArgs : CancelEventArgs
  {
    private Cursor m_cursor;
    private Strokes m_strokes;
    private Gesture[] m_gestures;

    public Cursor Cursor => this.m_cursor;

    public Strokes Strokes => this.m_strokes;

    public Gesture[] Gestures => (Gesture[]) this.m_gestures.Clone();

    public InkEditGestureEventArgs(
      Cursor cursor,
      Strokes strokes,
      Gesture[] gestures,
      bool cancel)
      : base(cancel)
    {
      this.m_cursor = cursor;
      this.m_strokes = strokes;
      this.m_gestures = gestures;
    }
  }
}
