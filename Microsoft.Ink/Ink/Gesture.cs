// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Gesture
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Drawing;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Gesture
  {
    internal IInkGesture m_Gesture;

    internal Gesture(IInkGesture gesture) => this.m_Gesture = gesture;

    private Gesture()
    {
    }

    public ApplicationGesture Id => (ApplicationGesture) this.m_Gesture.Id;

    public Point HotPoint
    {
      get
      {
        int x = 0;
        int y = 0;
        this.m_Gesture.GetHotPoint(ref x, ref y);
        return new Point(x, y);
      }
    }

    public RecognitionConfidence Confidence => (RecognitionConfidence) this.m_Gesture.Confidence;
  }
}
