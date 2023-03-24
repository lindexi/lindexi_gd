// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Recognizer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Recognizer
  {
    internal IInkRecognizer m_Recognizer;

    internal Recognizer(IInkRecognizer recognizer) => this.m_Recognizer = recognizer;

    private Recognizer()
    {
    }

    public string Name => this.m_Recognizer.Name;

    public override string ToString() => this.Name;

    public string Vendor => this.m_Recognizer.Vendor;

    public RecognizerCapabilities Capabilities => (RecognizerCapabilities) this.m_Recognizer.Capabilities;

    public Guid[] PreferredPacketDescription
    {
      get
      {
        string[] packetDescription1 = (string[]) this.m_Recognizer.PreferredPacketDescription;
        Guid[] packetDescription2 = new Guid[packetDescription1.Length];
        for (int index = 0; index < packetDescription1.Length; ++index)
          packetDescription2[index] = new Guid(packetDescription1[index]);
        return packetDescription2;
      }
    }

    public Guid Id => new Guid(((IInkRecognizer2) this.m_Recognizer).Id);

    public UnicodeRange[] GetUnicodeRanges() => InkHelperMethods.IntToRanges((int[]) ((IInkRecognizer2) this.m_Recognizer).UnicodeRanges);

    public Guid[] SupportedProperties
    {
      get
      {
        string[] supportedProperties1 = (string[]) this.m_Recognizer.SupportedProperties;
        Guid[] supportedProperties2 = new Guid[supportedProperties1.Length];
        for (int index = 0; index < supportedProperties1.Length; ++index)
          supportedProperties2[index] = new Guid(supportedProperties1[index]);
        return supportedProperties2;
      }
    }

    public short[] Languages => (short[]) this.m_Recognizer.Languages;

    public RecognizerContext CreateRecognizerContext() => new RecognizerContext(this.m_Recognizer.CreateRecognizerContext());
  }
}
