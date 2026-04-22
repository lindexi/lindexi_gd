namespace JiyinunalcheWaqerehoqarlijear;

class PowerPointReader
{
    public async Task<IReadOnlyList<PowerPointSlideInfo>> ReadSlidesAsync(FileInfo pptxFile)
    {
        await Task.CompletedTask;

        return [];
    }
}

record PowerPointSlideInfo(int SlideIndex, string SlideText, FileInfo SlideImageFile);