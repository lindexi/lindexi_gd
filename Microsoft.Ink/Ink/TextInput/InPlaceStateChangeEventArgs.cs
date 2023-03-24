// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TextInput.InPlaceStateChangeEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink.TextInput
{
  public class InPlaceStateChangeEventArgs : EventArgs
  {
    private InPlaceState oldState;
    private InPlaceState newState;

    public InPlaceStateChangeEventArgs(InPlaceState oldState, InPlaceState newState)
    {
      this.oldState = oldState;
      this.newState = newState;
    }

    public InPlaceState OldState => this.oldState;

    public InPlaceState NewState => this.newState;
  }
}
