// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Cursor
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Cursor
  {
    internal IInkCursor m_Cursor;
    private Tablet m_Tablet;
    private CursorButtons m_Buttons;

    internal Cursor(IInkCursor cursor) => this.m_Cursor = cursor;

    private Cursor()
    {
    }

    public string Name => this.m_Cursor.Name;

    public override string ToString() => this.Name;

    public int Id => this.m_Cursor.Id;

    public bool Inverted => this.m_Cursor.Inverted;

    public DrawingAttributes DrawingAttributes
    {
      get
      {
        lock (this)
        {
          IInkDrawingAttributes drawingAttributes = (IInkDrawingAttributes) this.m_Cursor.DrawingAttributes;
          return drawingAttributes == null ? (DrawingAttributes) null : new DrawingAttributes(new _InternalDrawingAttributes(drawingAttributes));
        }
      }
      set
      {
        lock (this)
        {
          if (value == null)
          {
            this.m_Cursor.DrawingAttributes = (InkDrawingAttributes) null;
          }
          else
          {
            try
            {
              this.m_Cursor.DrawingAttributes = (InkDrawingAttributes) value.m_DrawingAttributes;
            }
            catch (COMException ex)
            {
              InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
              throw;
            }
          }
        }
      }
    }

    public Tablet Tablet
    {
      get
      {
        if (this.m_Tablet == null)
          this.m_Tablet = new Tablet(this.m_Cursor.Tablet);
        return this.m_Tablet;
      }
    }

    public CursorButtons Buttons
    {
      get
      {
        if (this.m_Buttons == null)
          this.m_Buttons = new CursorButtons(this.m_Cursor.Buttons);
        return this.m_Buttons;
      }
    }
  }
}
