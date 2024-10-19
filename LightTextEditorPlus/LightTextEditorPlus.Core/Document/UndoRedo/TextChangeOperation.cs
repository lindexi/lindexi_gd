using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 文本变更的动作
/// </summary>
public class TextChangeOperation : TextOperation
{
    internal TextChangeOperation(TextEditorCore textEditor, Selection oldSelection, IImmutableRunList? oldRun, Selection newSelection, IImmutableRunList? newRun) : base(textEditor)
    {
        OldSelection = oldSelection;
        OldRun = oldRun;
        NewSelection = newSelection;
        NewRun = newRun;
    }

    private Selection OldSelection { get; }
    private IImmutableRunList? OldRun { get; }
    private Selection NewSelection { get; }
    private IImmutableRunList? NewRun { get; }

    /// <inheritdoc />
    public override TextOperationType TextOperationType => TextOperationType.ChangeTextData;

    /// <inheritdoc />
    protected override void OnUndo()
    {
        TextEditor.DocumentManager.EditAndReplaceRunList(NewSelection, OldRun);
    }

    /// <inheritdoc />
    protected override void OnRedo()
    {
        TextEditor.DocumentManager.EditAndReplaceRunList(OldSelection, NewRun);
    }
}
