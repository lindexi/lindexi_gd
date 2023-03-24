// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TextInput.TextInputPanelEventHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Microsoft.Ink.TextInput
{
  internal class TextInputPanelEventHelper : ITextInputPanelEventSink, IDisposable
  {
    private Microsoft.Ink.TextInputPanel unmanagedPanel;
    private TextInputPanel managedPanel;
    private bool disposed;
    private bool advised;
    private uint currentMask;

    internal TextInputPanelEventHelper(Microsoft.Ink.TextInputPanel unmanagedPanel, TextInputPanel managedPanel)
    {
      this.unmanagedPanel = unmanagedPanel;
      this.managedPanel = managedPanel;
    }

    ~TextInputPanelEventHelper() => this.Dispose(false);

    public void Dispose() => this.Dispose(true);

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void Dispose(bool explicitDispose)
    {
      if (this.disposed)
        return;
      this.disposed = true;
      GC.SuppressFinalize((object) this);
      if (this.unmanagedPanel == null)
        return;
      if (this.advised && explicitDispose)
        this.unmanagedPanel.Unadvise((ITextInputPanelEventSink) this);
      this.advised = false;
      this.unmanagedPanel = (Microsoft.Ink.TextInputPanel) null;
    }

    internal static Rectangle RectangleConversion(tagRECT toConvert) => new Rectangle(toConvert.Left, toConvert.Top, toConvert.Right - toConvert.Left, toConvert.Bottom - toConvert.Top);

    internal void AddEvent(EventMask mask)
    {
      lock (this)
      {
        if (this.disposed || this.unmanagedPanel == null)
          return;
        uint EventMask = (uint) ((EventMask) this.currentMask | mask);
        if ((int) EventMask == (int) this.currentMask)
          return;
        this.currentMask = EventMask;
        this.advised = true;
        this.unmanagedPanel.Advise((ITextInputPanelEventSink) this, EventMask);
      }
    }

    internal void RemoveEvent(EventMask mask)
    {
      lock (this)
      {
        if (this.disposed || this.unmanagedPanel == null)
          return;
        uint EventMask = (uint) ((EventMask) this.currentMask & ~mask);
        if ((int) EventMask == (int) this.currentMask)
          return;
        this.currentMask = EventMask;
        if (EventMask == 0U)
        {
          this.advised = false;
          this.unmanagedPanel.Unadvise((ITextInputPanelEventSink) this);
        }
        else
        {
          this.advised = true;
          this.unmanagedPanel.Advise((ITextInputPanelEventSink) this, EventMask);
        }
      }
    }

    public void InPlaceStateChanging(Microsoft.Ink.InPlaceState oldInPlaceState, Microsoft.Ink.InPlaceState newInPlaceState) => this.managedPanel.DoInPlaceStateChanging(new InPlaceStateChangeEventArgs((InPlaceState) oldInPlaceState, (InPlaceState) newInPlaceState));

    public void InPlaceStateChanged(Microsoft.Ink.InPlaceState oldInPlaceState, Microsoft.Ink.InPlaceState newInPlaceState) => this.managedPanel.DoInPlaceStateChanged(new InPlaceStateChangeEventArgs((InPlaceState) oldInPlaceState, (InPlaceState) newInPlaceState));

    public void InPlaceSizeChanging(tagRECT oldBoundingRectangle, tagRECT newBoundingRectangle) => this.managedPanel.DoInPlaceSizeChanging(new InPlaceSizeChangeEventArgs(TextInputPanelEventHelper.RectangleConversion(oldBoundingRectangle), TextInputPanelEventHelper.RectangleConversion(newBoundingRectangle)));

    public void InPlaceSizeChanged(tagRECT oldBoundingRectangle, tagRECT newBoundingRectangle) => this.managedPanel.DoInPlaceSizeChanged(new InPlaceSizeChangeEventArgs(TextInputPanelEventHelper.RectangleConversion(oldBoundingRectangle), TextInputPanelEventHelper.RectangleConversion(newBoundingRectangle)));

    public void InputAreaChanging(Microsoft.Ink.PanelInputArea oldInputArea, Microsoft.Ink.PanelInputArea newInputArea) => this.managedPanel.DoInputAreaChanging(new InputAreaChangeEventArgs((PanelInputArea) oldInputArea, (PanelInputArea) newInputArea));

    public void InputAreaChanged(Microsoft.Ink.PanelInputArea oldInputArea, Microsoft.Ink.PanelInputArea newInputArea) => this.managedPanel.DoInputAreaChanged(new InputAreaChangeEventArgs((PanelInputArea) oldInputArea, (PanelInputArea) newInputArea));

    public void CorrectionModeChanging(
      Microsoft.Ink.CorrectionMode oldCorrectionMode,
      Microsoft.Ink.CorrectionMode newCorrectionMode)
    {
      this.managedPanel.DoCorrectionModeChanging(new CorrectionModeChangeEventArgs((CorrectionMode) oldCorrectionMode, (CorrectionMode) newCorrectionMode));
    }

    public void CorrectionModeChanged(
      Microsoft.Ink.CorrectionMode oldCorrectionMode,
      Microsoft.Ink.CorrectionMode newCorrectionMode)
    {
      this.managedPanel.DoCorrectionModeChanged(new CorrectionModeChangeEventArgs((CorrectionMode) oldCorrectionMode, (CorrectionMode) newCorrectionMode));
    }

    public void InPlaceVisibilityChanging(int oldVisible, int newVisible) => this.managedPanel.DoInPlaceVisibilityChanging(new InPlaceVisibilityChangeEventArgs(newVisible != 0));

    public void InPlaceVisibilityChanged(int oldVisible, int newVisible) => this.managedPanel.DoInPlaceVisibilityChanged(new InPlaceVisibilityChangeEventArgs(newVisible != 0));

    public void TextInserting(Array inkInterfaces)
    {
      if (inkInterfaces == null)
        return;
      int length = inkInterfaces.Length;
      Microsoft.Ink.Ink[] insertionInk = new Microsoft.Ink.Ink[length];
      for (int index = 0; index < length; ++index)
        insertionInk[index] = new Microsoft.Ink.Ink((InkDisp) inkInterfaces.GetValue(index));
      this.managedPanel.DoTextInserting(new TextInsertionEventArgs(insertionInk));
    }

    public void TextInserted(Array inkInterfaces)
    {
      if (inkInterfaces == null)
        return;
      int length = inkInterfaces.Length;
      Microsoft.Ink.Ink[] insertionInk = new Microsoft.Ink.Ink[length];
      for (int index = 0; index < length; ++index)
        insertionInk[index] = new Microsoft.Ink.Ink((InkDisp) inkInterfaces.GetValue(index));
      this.managedPanel.DoTextInserted(new TextInsertionEventArgs(insertionInk));
    }
  }
}
