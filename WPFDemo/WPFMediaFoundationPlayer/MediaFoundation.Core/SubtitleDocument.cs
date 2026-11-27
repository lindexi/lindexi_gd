using System.Text;

namespace MediaFoundation.Core;

/// <summary>使用左闭右开时间区间的不可变字幕片段；加入文档时验证内容。</summary>
public sealed record SubtitleCue(TimeSpan Start, TimeSpan End, string Text);

/// <summary>与文件格式无关的不可变文本字幕时间轴，可跨线程查询。</summary>
public sealed class SubtitleDocument
{
    private readonly SubtitleCue[] _cues;
    private readonly TimeSpan[] _maximumEnds;

    /// <summary>复制并按开始时间稳定排序片段。允许空集合及重叠片段；时间须非负、结束晚于开始且文字非空。</summary>
    public SubtitleDocument(IEnumerable<SubtitleCue> cues)
    {
        ArgumentNullException.ThrowIfNull(cues);
        var snapshot = cues.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            var cue = snapshot[i];
            if (cue is null || cue.Start < TimeSpan.Zero || cue.End <= cue.Start || string.IsNullOrWhiteSpace(cue.Text))
                throw new ArgumentException(SR.Current.SubtitleInvalidCue.ToString(i), nameof(cues));
        }
        _cues = snapshot.OrderBy(cue => cue.Start).ToArray();
        Cues = Array.AsReadOnly(_cues);
        _maximumEnds = new TimeSpan[_cues.Length];
        TimeSpan end = TimeSpan.Zero;
        for (int i = 0; i < _cues.Length; i++)
        {
            if (_cues[i].End > end) end = _cues[i].End;
            _maximumEnds[i] = end;
        }
    }

    /// <summary>按开始时间稳定排序的只读片段快照。</summary>
    public IReadOnlyList<SubtitleCue> Cues { get; }

    /// <summary>返回指定媒体位置的活动字幕，重叠片段按开始时间以换行分隔。</summary>
    public string GetText(TimeSpan position)
    {
        int low = 0, high = _cues.Length;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (_cues[middle].Start <= position) low = middle + 1;
            else high = middle;
        }
        int last = low - 1;
        int first = last;
        while (first >= 0 && _maximumEnds[first] > position) first--;
        string? single = null;
        StringBuilder? combined = null;
        for (int i = first + 1; i <= last; i++)
        {
            if (_cues[i].End <= position) continue;
            if (single == null) single = _cues[i].Text;
            else
            {
                combined ??= new StringBuilder(single);
                combined.Append('\n').Append(_cues[i].Text);
            }
        }
        return combined?.ToString() ?? single ?? string.Empty;
    }
}
