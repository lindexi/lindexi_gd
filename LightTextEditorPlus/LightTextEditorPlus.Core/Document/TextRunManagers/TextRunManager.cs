using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document.DocumentManagers;

namespace LightTextEditorPlus.Core.Document;

internal class TextRunManager
{
    public TextRunManager(DocumentManager documentManager)
    {
        
    }

    public ParagraphManager ParagraphManager { get; } = new ParagraphManager();
}

/// <summary>
/// 段落管理
/// </summary>
class ParagraphManager
{
    public List<ParagraphData> ParagraphList { get; } = new List<ParagraphData>();
}

/// <summary>
/// 段落数据
/// </summary>
class ParagraphData
{
    // todo 实现默认的段落数据
    public IReadonlyParagraphProperty ParagraphProperty { set; get; }
    public List<ITextRun> TextRunList { get; } = new List<ITextRun>();
}