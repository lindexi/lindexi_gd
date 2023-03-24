// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.SystemGestureData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System.Drawing;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class SystemGestureData
  {
    private Stylus m_Stylus;
    private SystemGesture m_Id;
    private Point m_pt;
    private int m_Modifier;
    private char m_Character;
    private int m_StylusMode;

    private SystemGestureData()
    {
    }

    internal SystemGestureData(
      Stylus stylus,
      SystemGesture id,
      Point pt,
      int modifier,
      char character,
      int stylusMode)
    {
      this.m_Stylus = stylus;
      this.m_Id = id;
      this.m_pt = pt;
      this.m_Modifier = modifier;
      this.m_Character = character;
      this.m_StylusMode = stylusMode;
    }

    public Stylus Stylus => this.m_Stylus;

    public SystemGesture Id => this.m_Id;

    public Point Point => this.m_pt;

    public int Modifier => this.m_Modifier;

    public char Character => this.m_Character;

    public int StylusMode => this.m_StylusMode;
  }
}
