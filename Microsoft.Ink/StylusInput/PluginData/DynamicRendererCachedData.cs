// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.DynamicRendererCachedData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.StylusInput.PluginData
{
  public sealed class DynamicRendererCachedData
  {
    private int m_cachedDataId;
    private DynamicRenderer m_renderer;

    internal DynamicRendererCachedData(int cachedDataId, DynamicRenderer renderer)
    {
      this.m_cachedDataId = cachedDataId;
      this.m_renderer = renderer;
    }

    public int CachedDataId => this.m_cachedDataId;

    public DynamicRenderer DynamicRenderer => this.m_renderer;
  }
}
