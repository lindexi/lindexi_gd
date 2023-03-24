// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TextInput.InPlaceSizeChangeEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;

namespace Microsoft.Ink.TextInput
{
  public class InPlaceSizeChangeEventArgs : EventArgs
  {
    private Rectangle oldSize = Rectangle.Empty;
    private Rectangle newSize = Rectangle.Empty;

    public InPlaceSizeChangeEventArgs(Rectangle oldSize, Rectangle newSize)
    {
      this.oldSize = oldSize;
      this.newSize = newSize;
    }

    public Rectangle OldSize => this.oldSize;

    public Rectangle NewSize => this.newSize;
  }
}
