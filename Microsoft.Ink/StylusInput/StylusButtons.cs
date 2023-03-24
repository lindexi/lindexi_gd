// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.StylusButtons
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;

namespace Microsoft.StylusInput
{
  public sealed class StylusButtons
  {
    private int m_Count;
    internal Guid[] m_IdsOfButtons;
    internal string[] m_NamesOfButtons;

    internal StylusButtons(int count, Guid[] ids, string[] names)
    {
      if (count == 0 || ids.Length != count || names.Length != count)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InconsistentArguments"));
      this.m_Count = count;
      this.m_IdsOfButtons = ids;
      this.m_NamesOfButtons = names;
    }

    public int Count => this.m_Count;

    public Guid GetId(int index) => index >= 0 && index < this.m_Count ? this.m_IdsOfButtons[index] : throw new ArgumentOutOfRangeException(nameof (index), Helpers.SharedResources.Errors.GetString("IndexOutOfRange"));

    public string GetName(int index) => index >= 0 && index < this.m_Count ? this.m_NamesOfButtons[index] : throw new ArgumentOutOfRangeException(nameof (index), Helpers.SharedResources.Errors.GetString("IndexOutOfRange"));
  }
}
