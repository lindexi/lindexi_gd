// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.UnicodeRange
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink
{
  public struct UnicodeRange
  {
    private char m_startingCharacter;
    private int m_length;

    public UnicodeRange(char startingCharacter, int length)
    {
      this.m_startingCharacter = startingCharacter;
      this.m_length = length;
    }

    public char StartingCharacter
    {
      get => this.m_startingCharacter;
      set => this.m_startingCharacter = value;
    }

    public int Length
    {
      get => this.m_length;
      set => this.m_length = value >= 0 && value <= (int) ushort.MaxValue ? value : throw new ArgumentOutOfRangeException(nameof (value));
    }

    public static bool operator ==(UnicodeRange x, UnicodeRange y) => (int) x.m_startingCharacter == (int) y.m_startingCharacter && x.m_length == y.m_length;

    public static bool operator !=(UnicodeRange x, UnicodeRange y) => !(x == y);

    public override bool Equals(object obj) => obj is UnicodeRange unicodeRange && this == unicodeRange;

    public override int GetHashCode() => this.m_startingCharacter.GetHashCode() ^ this.m_length.GetHashCode();
  }
}
