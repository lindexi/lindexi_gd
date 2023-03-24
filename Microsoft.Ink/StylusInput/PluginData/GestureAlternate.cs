// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.GestureAlternate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class GestureAlternate
  {
    private ApplicationGesture m_Id;
    private RecognitionConfidence m_Confidence;
    private int m_strokeCount;

    internal GestureAlternate(GestureAlternateNative gdn)
    {
      this.m_Id = (ApplicationGesture) gdn.gestureId;
      this.m_Confidence = (RecognitionConfidence) gdn.recoConfidence;
      this.m_strokeCount = gdn.strokeCount;
    }

    public ApplicationGesture Id => this.m_Id;

    public RecognitionConfidence Confidence => this.m_Confidence;

    public int StrokeCount => this.m_strokeCount;
  }
}
