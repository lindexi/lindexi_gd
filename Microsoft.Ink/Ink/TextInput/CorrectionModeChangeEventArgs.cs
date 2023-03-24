// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TextInput.CorrectionModeChangeEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink.TextInput
{
  public class CorrectionModeChangeEventArgs : EventArgs
  {
    private CorrectionMode oldMode;
    private CorrectionMode newMode;

    public CorrectionModeChangeEventArgs(CorrectionMode oldMode, CorrectionMode newMode)
    {
      this.oldMode = oldMode;
      this.newMode = newMode;
    }

    public CorrectionMode OldMode => this.oldMode;

    public CorrectionMode NewMode => this.newMode;
  }
}
