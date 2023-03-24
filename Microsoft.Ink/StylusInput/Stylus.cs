// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.Stylus
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.StylusInput
{
  public sealed class Stylus
  {
    private bool m_Inverted;
    private int m_Id;
    private int m_tcid;
    private string m_Name;
    private StylusButtons m_Buttons;

    private Stylus()
    {
    }

    internal Stylus(
      int id,
      int tabletContextId,
      bool inverted,
      string name,
      StylusButtons buttons)
    {
      this.m_Id = id;
      this.m_tcid = tabletContextId;
      this.m_Inverted = inverted;
      this.m_Name = name;
      this.m_Buttons = buttons;
    }

    public bool Inverted => this.m_Inverted;

    public int Id => this.m_Id;

    public string Name => this.m_Name;

    public int TabletContextId => this.m_tcid;

    public StylusButtons Buttons => this.m_Buttons;

    internal void SetTabletContextId(int tcid) => this.m_tcid = tcid;
  }
}
