// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.IStylusAsyncPlugin
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.StylusInput.PluginData;

namespace Microsoft.StylusInput
{
  public interface IStylusAsyncPlugin
  {
    void RealTimeStylusEnabled(RealTimeStylus sender, RealTimeStylusEnabledData data);

    void RealTimeStylusDisabled(RealTimeStylus sender, RealTimeStylusDisabledData data);

    void StylusInRange(RealTimeStylus sender, StylusInRangeData data);

    void StylusOutOfRange(RealTimeStylus sender, StylusOutOfRangeData data);

    void StylusDown(RealTimeStylus sender, StylusDownData data);

    void StylusUp(RealTimeStylus sender, StylusUpData data);

    void StylusButtonDown(RealTimeStylus sender, StylusButtonDownData data);

    void StylusButtonUp(RealTimeStylus sender, StylusButtonUpData data);

    void InAirPackets(RealTimeStylus sender, InAirPacketsData data);

    void Packets(RealTimeStylus sender, PacketsData data);

    void SystemGesture(RealTimeStylus sender, SystemGestureData data);

    void TabletAdded(RealTimeStylus sender, TabletAddedData data);

    void TabletRemoved(RealTimeStylus sender, TabletRemovedData data);

    void CustomStylusDataAdded(RealTimeStylus sender, CustomStylusData data);

    void Error(RealTimeStylus sender, ErrorData data);

    DataInterestMask DataInterest { get; }
  }
}
