// See https://aka.ms/new-console-template for more information

using System.Buffers;

var init = ArrayPool<byte>.Shared;

var allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();

var glyphMetricsArray = ArrayPool<GlyphMetrics>.Shared.Rent(16);

var lastAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();

var n = lastAllocatedBytesForCurrentThread - allocatedBytesForCurrentThread;

var dit = new GlyphMetrics[16];

var ditAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();

var m = ditAllocatedBytesForCurrentThread - lastAllocatedBytesForCurrentThread;

;

class GlyphMetrics
{
    public float Width { set; get; }
    public float Height { set; get; }
}