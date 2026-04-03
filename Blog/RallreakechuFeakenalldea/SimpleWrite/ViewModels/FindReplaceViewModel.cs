using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;

using SimpleWrite.Business.FindReplaces;
using SimpleWrite.Business.FolderExplorers;

namespace SimpleWrite.ViewModels;

/// <summary>
/// 查找和替换的模型
/// </summary>
public class FindReplaceViewModel : ViewModelBase
{
    private readonly FolderSearchService _folderSearchService = new FolderSearchService();

    public FindReplaceViewModel(EditorViewModel editorViewModel, FolderExplorerViewModel folderExplorerViewModel)
    {
        ArgumentNullException.ThrowIfNull(editorViewModel);
        ArgumentNullException.ThrowIfNull(folderExplorerViewModel);

        EditorViewModel = editorViewModel;
        FolderExplorerViewModel = folderExplorerViewModel;
        FolderExplorerViewModel.CurrentFolderChanged += FolderExplorerViewModelOnCurrentFolderChanged;
    }

    public bool IsPanelVisible
    {
        get => _isPanelVisible;
        private set
        {
            if (SetField(ref _isPanelVisible, value))
            {
                OnPropertyChanged(nameof(IsFolderSearchResultVisible));
                UpdateSearchStatus();
            }
        }
    }

    public bool IsReplaceMode
    {
        get => _isReplaceMode;
        private set
        {
            if (SetField(ref _isReplaceMode, value))
            {
                OnPropertyChanged(nameof(IsReplaceAvailable));
            }
        }
    }

    public bool IsFolderSearchScope
    {
        get => _isFolderSearchScope;
        set
        {
            if (!SetField(ref _isFolderSearchScope, value))
            {
                return;
            }

            ClearFolderSearchResults();
            _hasExecutedFolderSearch = false;
            OnPropertyChanged(nameof(IsDocumentSearchScope));
            OnPropertyChanged(nameof(IsFolderSearchResultVisible));
            OnPropertyChanged(nameof(CanNavigateDocumentMatches));
            OnPropertyChanged(nameof(IsReplaceAvailable));
            OnPropertyChanged(nameof(CanSearchInFolder));

            if (!value)
            {
                RefreshSearchMatches();
                return;
            }

            UpdateSearchStatus();
        }
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

            RefreshSearchState(clearFolderSearchResults: true);
        }
    }

    public string ReplaceText
    {
        get => _replaceText;
        set => SetField(ref _replaceText, value);
    }

    public bool IsCaseInsensitive
    {
        get => _isCaseInsensitive;
        set
        {
            if (!SetField(ref _isCaseInsensitive, value))
            {
                return;
            }

            RefreshSearchState(clearFolderSearchResults: true);
        }
    }

    public bool UseRegularExpression
    {
        get => _useRegularExpression;
        set
        {
            if (!SetField(ref _useRegularExpression, value))
            {
                return;
            }

            RefreshSearchState(clearFolderSearchResults: true);
        }
    }

    public bool HasSearchText => !string.IsNullOrEmpty(FindText);

    public bool HasValidSearchPattern => string.IsNullOrEmpty(FindText) || _searchMatcher is not null;

    public bool HasMatches => _searchMatchList.Count > 0;

    public bool HasFolderSearchResults => FolderSearchResultList.Count > 0;

    public bool IsFolderSearchAvailable => FolderExplorerViewModel.CurrentFolder is not null;

    public bool IsFolderSearchResultVisible => IsPanelVisible && IsFolderSearchScope;

    public bool IsDocumentSearchScope => !IsFolderSearchScope;

    public bool CanNavigateDocumentMatches => HasSearchText && HasValidSearchPattern && !IsFolderSearchScope;

    public bool IsReplaceAvailable => IsReplaceMode && !IsFolderSearchScope;

    public bool CanSearchInFolder => IsFolderSearchScope && IsFolderSearchAvailable && HasSearchText && HasValidSearchPattern && !IsSearchingFolder;

    public bool IsSearchingFolder
    {
        get => _isSearchingFolder;
        private set
        {
            if (SetField(ref _isSearchingFolder, value))
            {
                OnPropertyChanged(nameof(CanSearchInFolder));
                UpdateSearchStatus();
            }
        }
    }

    public ObservableCollection<FolderSearchResultViewModel> FolderSearchResultList { get; } = [];

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

    public async Task SearchInFolderAsync()
    {
        if (!IsFolderSearchScope || !IsFolderSearchAvailable || string.IsNullOrEmpty(FindText) || !HasValidSearchPattern || IsSearchingFolder)
        {
            UpdateSearchStatus();
            return;
        }

        var directoryInfo = FolderExplorerViewModel.CurrentFolder;
        if (directoryInfo is null)
        {
            UpdateSearchStatus();
            return;
        }

        IsSearchingFolder = true;
        try
        {
            var searchMatcher = _searchMatcher;
            if (searchMatcher is null)
            {
                _hasExecutedFolderSearch = false;
                UpdateSearchStatus();
                return;
            }

            var resultList = await _folderSearchService.SearchAsync(directoryInfo, searchMatcher);

            ClearFolderSearchResults();
            foreach (var folderSearchResult in resultList)
            {
                FolderSearchResultList.Add(new FolderSearchResultViewModel(folderSearchResult));
            }

            _hasExecutedFolderSearch = true;
            OnPropertyChanged(nameof(HasFolderSearchResults));
        }
        finally
        {
            IsSearchingFolder = false;
            UpdateSearchStatus();
        }
    }

    public async Task OpenFolderSearchResultAsync(FolderSearchResultViewModel? folderSearchResult)
    {
        if (folderSearchResult is null)
        {
            return;
        }

        await EditorViewModel.OpenFileAsync(folderSearchResult.FileInfo);
        RefreshCurrentEditor();

        if (!TryGetCurrentTextEditor(out var textEditor))
        {
            return;
        }

        var startOffset = new CaretOffset(folderSearchResult.FirstMatchOffset);
        var endOffset = new CaretOffset(folderSearchResult.FirstMatchOffset + folderSearchResult.MatchLength);
        textEditor.CurrentSelection = new Selection(startOffset, endOffset);
        textEditor.Focus();
        UpdateSearchStatus();
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
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        var nextIndex = GetNextMatchIndex(textEditor.CurrentSelection);
        ApplyMatch(textEditor, nextIndex);
    }

    public void FindPrevious()
    {
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        var previousIndex = GetPreviousMatchIndex(textEditor.CurrentSelection);
        ApplyMatch(textEditor, previousIndex);
    }

    public void ReplaceCurrent()
    {
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        var matchIndex = TryGetExactMatchIndex(textEditor.CurrentSelection);
        if (matchIndex < 0)
        {
            matchIndex = GetNextMatchIndex(textEditor.CurrentSelection);
        }

        var searchMatcher = _searchMatcher;
        if (searchMatcher is null)
        {
            return;
        }

        var documentText = textEditor.Text;
        var match = _searchMatchList[matchIndex];
        var replacementText = searchMatcher.GetReplacementText(documentText, match.ToSearchMatchResult(), ReplaceText);
        var nextAnchorOffset = match.StartOffset + replacementText.Length;
        textEditor.EditAndReplace(replacementText, match.ToSelection());

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
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || _searchMatchList.Count == 0)
        {
            return;
        }

        var searchMatcher = _searchMatcher;
        if (searchMatcher is null)
        {
            return;
        }

        var documentText = textEditor.Text;
        for (var i = _searchMatchList.Count - 1; i >= 0; i--)
        {
            var match = _searchMatchList[i];
            var replacementText = searchMatcher.GetReplacementText(documentText, match.ToSearchMatchResult(), ReplaceText);
            textEditor.EditAndReplace(replacementText, match.ToSelection());
        }

        RefreshSearchMatches();
    }

    private EditorViewModel EditorViewModel { get; }

    private FolderExplorerViewModel FolderExplorerViewModel { get; }

    private void ShowPanel(bool isReplaceMode)
    {
        IsReplaceMode = isReplaceMode;
        IsPanelVisible = true;

        TryUseCurrentSelectionAsFindText();
        RefreshCurrentEditor();
    }

    /// <summary>
    /// 尝试使用当前文本的选择内容作为查找内容
    /// </summary>
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
            // 如果是超过一段的内容，则忽略，太长了估计也不会作为查找的内容
            return;
        }

        FindText = selectedText;
    }

    /// <summary>
    /// 尝试找到当前的文本框
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
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

        if (IsFolderSearchScope)
        {
            NotifyMatchStateChanged();
            UpdateSearchStatus();
            return;
        }

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

        var searchMatcher = _searchMatcher;
        if (searchMatcher is null)
        {
            NotifyMatchStateChanged();
            UpdateSearchStatus();
            return;
        }

        foreach (var searchMatch in searchMatcher.FindMatches(textEditor.Text))
        {
            _searchMatchList.Add(new SearchMatch(searchMatch.StartOffset, searchMatch.Length, searchMatch.Value));
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

        if (!HasValidSearchPattern)
        {
            SetSearchStatusText(IsFolderSearchScope ? "[文件夹查找: 正则表达式无效]" : "[查找: 正则表达式无效]");
            return;
        }

        if (IsFolderSearchScope)
        {
            if (!IsFolderSearchAvailable)
            {
                SetSearchStatusText("[文件夹查找: 未打开文件夹]");
                return;
            }

            if (IsSearchingFolder)
            {
                SetSearchStatusText("[文件夹查找: 搜索中]");
                return;
            }

            if (!_hasExecutedFolderSearch)
            {
                SetSearchStatusText("[文件夹查找: 按 Enter 执行搜索]");
                return;
            }

            if (FolderSearchResultList.Count == 0)
            {
                SetSearchStatusText("[文件夹查找: 0 个文件命中]");
                return;
            }

            SetSearchStatusText($"[文件夹查找: {FolderSearchResultList.Count} 个文件，{FolderSearchResultList.Sum(temp => temp.MatchCount)} 处命中]");
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

    private void FolderExplorerViewModelOnCurrentFolderChanged(object? sender, EventArgs e)
    {
        ClearFolderSearchResults();
        _hasExecutedFolderSearch = false;
        OnPropertyChanged(nameof(IsFolderSearchAvailable));
        OnPropertyChanged(nameof(CanSearchInFolder));
        UpdateSearchStatus();
    }

    private void ClearFolderSearchResults()
    {
        if (FolderSearchResultList.Count == 0)
        {
            return;
        }

        FolderSearchResultList.Clear();
        OnPropertyChanged(nameof(HasFolderSearchResults));
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

    private void RefreshSearchState(bool clearFolderSearchResults)
    {
        RefreshSearchMatcher();
        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(HasValidSearchPattern));
        OnPropertyChanged(nameof(CanNavigateDocumentMatches));
        OnPropertyChanged(nameof(CanSearchInFolder));

        if (clearFolderSearchResults)
        {
            ClearFolderSearchResults();
            _hasExecutedFolderSearch = false;
        }

        if (IsFolderSearchScope)
        {
            UpdateSearchStatus();
            return;
        }

        RefreshSearchMatches();
    }

    private void RefreshSearchMatcher()
    {
        if (string.IsNullOrEmpty(FindText))
        {
            _searchMatcher = null;
            return;
        }

        _searchMatcher = SearchMatcher.TryCreate(FindText, UseRegularExpression, IsCaseInsensitive, out var searchMatcher)
            ? searchMatcher
            : null;
    }

    private readonly List<SearchMatch> _searchMatchList = [];
    private TextEditor? _currentTextEditor;
    private bool _isPanelVisible;
    private bool _isReplaceMode;
    private bool _isFolderSearchScope;
    private bool _isCaseInsensitive;
    private bool _isSearchingFolder;
    private bool _useRegularExpression;
    private bool _hasExecutedFolderSearch;
    private string _findText = string.Empty;
    private string _replaceText = string.Empty;
    private string _searchStatusText = string.Empty;
    private SearchMatcher? _searchMatcher;

    private readonly record struct SearchMatch(int StartOffset, int Length, string Value)
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

        public SearchMatchResult ToSearchMatchResult()
        {
            return new SearchMatchResult(StartOffset, Length, Value);
        }
    }
}
