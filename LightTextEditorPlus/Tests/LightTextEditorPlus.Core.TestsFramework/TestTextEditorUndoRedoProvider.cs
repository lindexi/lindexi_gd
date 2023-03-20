using System.Collections.Generic;
using System.Linq;

using LightTextEditorPlus.Core.Document.UndoRedo;

namespace LightTextEditorPlus.Core.TestsFramework;

public class TestTextEditorUndoRedoProvider : ITextEditorUndoRedoProvider
{
    public void Insert(ITextOperation textOperation)
    {
        UndoOperationList.Add(textOperation);
        RedoOperationList.Clear();
    }

    public void Undo()
    {
        var last = UndoOperationList.LastOrDefault();
        if (last != null)
        {
            last.Undo();
            UndoOperationList.RemoveAt(UndoOperationList.Count - 1);
            RedoOperationList.Add(last);
        }
    }

    public void Redo()
    {
        var last = RedoOperationList.LastOrDefault();
        if (last != null)
        {
            last.Redo();
            RedoOperationList.RemoveAt(RedoOperationList.Count - 1);
            UndoOperationList.Add(last);
        }
    }

    public List<ITextOperation> UndoOperationList { set; get; } = new List<ITextOperation>();

    public List<ITextOperation> RedoOperationList { set; get; } = new List<ITextOperation>();
}