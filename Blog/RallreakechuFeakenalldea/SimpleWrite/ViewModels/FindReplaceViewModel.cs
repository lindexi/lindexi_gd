using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;

namespace SimpleWrite.ViewModels;

public class FindReplaceViewModel : ViewModelBase
{
    public FindReplaceViewModel(EditorViewModel editorViewModel)
    {
        ArgumentNullException.ThrowIfNull(editorViewModel);
        EditorViewModel = editorViewModel;
    }

    public bool IsPanelVisible
    {
        get => _isPanelVisible;
        private set
        {
            if (SetField(ref _isPanelVisible, value))
            {
                UpdateSearchStatus();
            }
        }
    }

    public bool IsReplaceMode
    {
        get => _isReplaceMode;
        private set => SetField(ref _isReplaceMode, value);
    }

    public string FindText
    {
        get => _findText;
        set
        {
            if (!SetField(ref _findText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSearchText));
            RefreshSearchMatches();
        }
    }

    public string ReplaceText
    {
        get => _replaceText;
        set => SetField(ref _replaceText, value);
    }

    public bool HasSearchText => !string.IsNullOrEmpty(FindText);

    public bool HasMatches => _searchMatchList.Count > 0;

    public string SearchStatusText => _searchStatusText;

    public void ShowFind()
    {
        ShowPanel(isReplaceMode: false);
    }

    public void ShowReplace()
    {
        ShowPanel(isReplaceMode: true);
    }

    public void Hide()
    {
        IsPanelVisible = false;
    }

    public void RefreshCurrentEditor()
    {
        var textEditor = EditorViewModel.CurrentEditorModel.TextEditor;
        if (ReferenceEquals(_currentTextEditor, textEditor))
        {
            RefreshSearchMatches();
            return;
        }

        if (_currentTextEditor is not null)
        {
            _currentTextEditor.TextEditorCore.TextChanged -= CurrentTextEditorOnTextChanged;
            _currentTextEditor.CurrentSelectionChanged -= CurrentTextEditorOnCurrentSelectionChanged;
        }

        _currentTextEditor = textEditor;

        if (_currentTextEditor is not null)
        {
            _currentTextEditor.TextEditorCore.TextChanged += CurrentTextEditorOnTextChanged;
            _currentTextEditor.CurrentSelectionChanged += CurrentTextEditorOnCurrentSelectionChanged;
        }

        RefreshSearchMatches();
    }

    public void FindNext()
    {
        if (!TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        var nextIndex = GetNextMatchIndex(textEditor.CurrentSelection);
        ApplyMatch(textEditor, nextIndex);
    }

    public void FindPrevious()
    {
        if (!TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        var previousIndex = GetPreviousMatchIndex(textEditor.CurrentSelection);
        ApplyMatch(textEditor, previousIndex);
    }

    public void ReplaceCurrent()
    {
        if (!TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        var matchIndex = TryGetExactMatchIndex(textEditor.CurrentSelection);
        if (matchIndex < 0)
        {
            matchIndex = GetNextMatchIndex(textEditor.CurrentSelection);
        }

        var match = _searchMatchList[matchIndex];
        var nextAnchorOffset = match.StartOffset + ReplaceText.Length;
        textEditor.EditAndReplace(ReplaceText, match.ToSelection());

        RefreshSearchMatches();

        if (_searchMatchList.Count == 0)
        {
            textEditor.CurrentCaretOffset = new CaretOffset(nextAnchorOffset);
            return;
        }

        var nextIndex = FindNextMatchIndexFromOffset(nextAnchorOffset);
        ApplyMatch(textEditor, nextIndex);
    }

    public void ReplaceAll()
    {
        if (!TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        for (var i = _searchMatchList.Count - 1; i >= 0; i--)
        {
            textEditor.EditAndReplace(ReplaceText, _searchMatchList[i].ToSelection());
        }

        RefreshSearchMatches();
    }

    private EditorViewModel EditorViewModel { get; }

    private void ShowPanel(bool isReplaceMode)
    {
        IsReplaceMode = isReplaceMode;
        IsPanelVisible = true;

        TryUseCurrentSelectionAsFindText();
        RefreshCurrentEditor();
    }

    private void TryUseCurrentSelectionAsFindText()
    {
        if (!TryGetCurrentTextEditor(out var textEditor))
        {
            return;
        }

        var selection = textEditor.CurrentSelection;
        if (selection.IsEmpty)
        {
            return;
        }

        var selectedText = textEditor.GetText(in selection);
        if (string.IsNullOrWhiteSpace(selectedText)
            || selectedText.Contains('\r')
            || selectedText.Contains('\n'))
        {
            return;
        }

        FindText = selectedText;
    }

    private bool TryGetCurrentTextEditor([NotNullWhen(true)] out TextEditor? textEditor)
    {
        textEditor = _currentTextEditor ?? EditorViewModel.CurrentEditorModel.TextEditor;
        if (textEditor is null)
        {
            _searchMatchList.Clear();
            NotifyMatchStateChanged();
            SetSearchStatusText(string.Empty);
            return false;
        }

        return true;
    }

    private void RefreshSearchMatches()
    {
        _searchMatchList.Clear();

        if (!TryGetCurrentTextEditor(out var textEditor))
        {
            return;
        }

        if (string.IsNullOrEmpty(FindText))
        {
            NotifyMatchStateChanged();
            UpdateSearchStatus();
            return;
        }

        var text = textEditor.Text;
        var findTextLength = FindText.Length;
        var startIndex = 0;

        while (startIndex <= text.Length - findTextLength)
        {
            var matchIndex = text.IndexOf(FindText, startIndex, StringComparison.Ordinal);
            if (matchIndex < 0)
            {
                break;
            }

            _searchMatchList.Add(new SearchMatch(matchIndex, findTextLength));
            startIndex = matchIndex + Math.Max(findTextLength, 1);
        }

        NotifyMatchStateChanged();
        UpdateSearchStatus();
    }

    private void ApplyMatch(TextEditor textEditor, int matchIndex)
    {
        if (_searchMatchList.Count == 0)
        {
            return;
        }

        var searchMatch = _searchMatchList[matchIndex];
        textEditor.CurrentSelection = searchMatch.ToSelection();
        UpdateSearchStatus();
    }

    private int GetNextMatchIndex(Selection selection)
    {
        var exactMatchIndex = TryGetExactMatchIndex(selection);
        if (exactMatchIndex >= 0)
        {
            return (exactMatchIndex + 1) % _searchMatchList.Count;
        }

        return FindNextMatchIndexFromOffset(selection.BehindOffset.Offset);
    }

    private int GetPreviousMatchIndex(Selection selection)
    {
        var exactMatchIndex = TryGetExactMatchIndex(selection);
        if (exactMatchIndex >= 0)
        {
            return (exactMatchIndex - 1 + _searchMatchList.Count) % _searchMatchList.Count;
        }

        return FindPreviousMatchIndexFromOffset(selection.FrontOffset.Offset);
    }

    private int FindNextMatchIndexFromOffset(int offset)
    {
        for (var i = 0; i < _searchMatchList.Count; i++)
        {
            if (_searchMatchList[i].StartOffset >= offset)
            {
                return i;
            }
        }

        return 0;
    }

    private int FindPreviousMatchIndexFromOffset(int offset)
    {
        for (var i = _searchMatchList.Count - 1; i >= 0; i--)
        {
            if (_searchMatchList[i].StartOffset < offset)
            {
                return i;
            }
        }

        return _searchMatchList.Count - 1;
    }

    private int TryGetExactMatchIndex(Selection selection)
    {
        for (var i = 0; i < _searchMatchList.Count; i++)
        {
            if (_searchMatchList[i].IsMatch(selection))
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateSearchStatus()
    {
        if (!IsPanelVisible || string.IsNullOrEmpty(FindText))
        {
            SetSearchStatusText(string.Empty);
            return;
        }

        var matchCount = _searchMatchList.Count;
        if (matchCount == 0)
        {
            SetSearchStatusText("[查找: 0 项]");
            return;
        }

        var currentMatchIndex = -1;
        if (TryGetCurrentTextEditor(out var textEditor))
        {
            currentMatchIndex = TryGetExactMatchIndex(textEditor.CurrentSelection);
            if (currentMatchIndex < 0)
            {
                currentMatchIndex = FindNextMatchIndexFromOffset(textEditor.CurrentSelection.FrontOffset.Offset);
            }
        }

        SetSearchStatusText($"[查找: {matchCount} 项，第 {currentMatchIndex + 1} 项]");
    }

    private void NotifyMatchStateChanged()
    {
        OnPropertyChanged(nameof(HasMatches));
    }

    private void SetSearchStatusText(string searchStatusText)
    {
        if (_searchStatusText == searchStatusText)
        {
            return;
        }

        _searchStatusText = searchStatusText;
        OnPropertyChanged(nameof(SearchStatusText));
    }

    private void CurrentTextEditorOnTextChanged(object? sender, EventArgs e)
    {
        RefreshSearchMatches();
    }

    private void CurrentTextEditorOnCurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        UpdateSearchStatus();
    }

    private readonly List<SearchMatch> _searchMatchList = [];
    private TextEditor? _currentTextEditor;
    private bool _isPanelVisible;
    private bool _isReplaceMode;
    private string _findText = string.Empty;
    private string _replaceText = string.Empty;
    private string _searchStatusText = string.Empty;

    private readonly record struct SearchMatch(int StartOffset, int Length)
    {
        public int EndOffset => StartOffset + Length;

        public Selection ToSelection()
        {
            return new Selection(new CaretOffset(StartOffset), new CaretOffset(EndOffset));
        }

        public bool IsMatch(Selection selection)
        {
            return selection.FrontOffset.Offset == StartOffset && selection.Length == Length;
        }
    }
}
