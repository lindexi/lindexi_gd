// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkOverlayPaintingEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.ComponentModel;
using System.Drawing;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class InkOverlayPaintingEventArgs : CancelEventArgs, IDisposable
  {
    private Graphics m_graphics;
    private Rectangle m_clipRect;

    public Graphics Graphics => this.m_graphics;

    public Rectangle ClipRectangle => this.m_clipRect;

    public InkOverlayPaintingEventArgs(Graphics graphics, Rectangle clipRectangle, bool Allow)
      : base(!Allow)
    {
      this.m_graphics = graphics;
      this.m_clipRect = clipRectangle;
    }

    ~InkOverlayPaintingEventArgs() => this.Dispose(false);

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing || this.m_graphics == null)
        return;
      this.m_graphics.Dispose();
    }
  }
}
