// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkRecognitionEvents_SinkHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ClassInterface(ClassInterfaceType.None)]
  [TypeLibType(TypeLibTypeFlags.FHidden)]
  internal sealed class _IInkRecognitionEvents_SinkHelper : InkEventForwarder, _IInkRecognitionEvents
  {
    public _IInkRecognitionEvents_RecognitionWithAlternatesEventHandler m_RecognitionWithAlternatesDelegate;
    public _IInkRecognitionEvents_RecognitionEventHandler m_RecognitionDelegate;
    public int m_dwCookie;

    public virtual void RecognitionWithAlternates(
      IInkRecognitionResult A_1,
      object A_2,
      InkRecognitionStatusPrivate A_3)
    {
      if (this.m_RecognitionWithAlternatesDelegate == null)
        return;
      this.m_RecognitionWithAlternatesDelegate(A_1, A_2, A_3);
    }

    public virtual void Recognition(string A_1, object A_2, InkRecognitionStatusPrivate A_3)
    {
      if (this.m_RecognitionDelegate == null)
        return;
      this.m_RecognitionDelegate(A_1, A_2, A_3);
    }

    internal _IInkRecognitionEvents_SinkHelper()
    {
      this.m_dwCookie = 0;
      this.m_RecognitionWithAlternatesDelegate = (_IInkRecognitionEvents_RecognitionWithAlternatesEventHandler) null;
      this.m_RecognitionDelegate = (_IInkRecognitionEvents_RecognitionEventHandler) null;
    }
  }
}
