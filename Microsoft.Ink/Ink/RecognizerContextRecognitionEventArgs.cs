// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognizerContextRecognitionEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class RecognizerContextRecognitionEventArgs : EventArgs
  {
    private string m_Text;
    private RecognitionStatus m_RecognitionStatus;
    private object m_CustomData;

    public string Text => this.m_Text;

    public object CustomData => this.m_CustomData;

    public RecognitionStatus RecognitionStatus => this.m_RecognitionStatus;

    public RecognizerContextRecognitionEventArgs(
      string text,
      object customData,
      RecognitionStatus recognitionStatus)
    {
      this.m_Text = text;
      this.m_CustomData = customData;
      this.m_RecognitionStatus = recognitionStatus;
    }
  }
}
