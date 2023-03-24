// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IPenInputPanelEvents_SinkHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [TypeLibType(TypeLibTypeFlags.FHidden)]
  [ClassInterface(ClassInterfaceType.None)]
  internal sealed class _IPenInputPanelEvents_SinkHelper : _IPenInputPanelEvents
  {
    public _IPenInputPanelEvents_InputFailedEventHandler m_InputFailedDelegate;
    public _IPenInputPanelEvents_VisibleChangedEventHandler m_VisibleChangedDelegate;
    public _IPenInputPanelEvents_PanelChangedEventHandler m_PanelChangedDelegate;
    public _IPenInputPanelEvents_PanelMovingEventHandler m_PanelMovingDelegate;
    public int m_dwCookie;

    public virtual void InputFailed(int A_1, int A_2, string A_3, short A_4)
    {
      if (this.m_InputFailedDelegate == null)
        return;
      this.m_InputFailedDelegate(A_1, A_2, A_3, A_4);
    }

    public virtual void VisibleChanged(bool A_1)
    {
      if (this.m_VisibleChangedDelegate == null)
        return;
      this.m_VisibleChangedDelegate(A_1);
    }

    public virtual void PanelChanged(PanelTypePrivate A_1)
    {
      if (this.m_PanelChangedDelegate == null)
        return;
      this.m_PanelChangedDelegate(A_1);
    }

    public virtual void PanelMoving(ref int A_1, ref int A_2)
    {
      if (this.m_PanelMovingDelegate == null)
        return;
      this.m_PanelMovingDelegate(ref A_1, ref A_2);
    }

    internal _IPenInputPanelEvents_SinkHelper()
    {
      this.m_dwCookie = 0;
      this.m_InputFailedDelegate = (_IPenInputPanelEvents_InputFailedEventHandler) null;
      this.m_VisibleChangedDelegate = (_IPenInputPanelEvents_VisibleChangedEventHandler) null;
      this.m_PanelChangedDelegate = (_IPenInputPanelEvents_PanelChangedEventHandler) null;
      this.m_PanelMovingDelegate = (_IPenInputPanelEvents_PanelMovingEventHandler) null;
    }
  }
}
