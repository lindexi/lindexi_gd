using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
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
            CancelPendingDocumentSearch();
            IsSearchingDocument = false;
            _searchMatchList = [];
            _isDocumentSearchTimedOut = false;
            NotifyMatchStateChanged();
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

    public bool HasMatches => _searchMatchList.Length > 0;

    public bool HasFolderSearchResults => FolderSearchResultList.Count > 0;

    public bool IsFolderSearchAvailable => FolderExplorerViewModel.CurrentFolder is not null;

    public bool IsFolderSearchResultVisible => IsPanelVisible && IsFolderSearchScope;

    public bool IsDocumentSearchScope => !IsFolderSearchScope;

    public bool CanNavigateDocumentMatches => HasSearchText && HasValidSearchPattern && !IsFolderSearchScope && !IsSearchingDocument;

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

    public bool IsSearchingDocument
    {
        get => _isSearchingDocument;
        private set
        {
            if (SetField(ref _isSearchingDocument, value))
            {
                OnPropertyChanged(nameof(CanNavigateDocumentMatches));
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
        CancelPendingDocumentSearch();
        IsSearchingDocument = false;
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
        _hasFolderSearchTimedOut = false;
        try
        {
            var searchMatcher = _searchMatcher;
            if (searchMatcher is null)
            {
                _hasExecutedFolderSearch = false;
                UpdateSearchStatus();
                return;
            }

            var folderSearchSummary = await _folderSearchService.SearchAsync(directoryInfo, searchMatcher);

            ClearFolderSearchResults();
            foreach (var folderSearchResult in folderSearchSummary.ResultList)
            {
                FolderSearchResultList.Add(new FolderSearchResultViewModel(folderSearchResult));
            }

            _hasExecutedFolderSearch = true;
            _hasFolderSearchTimedOut = folderSearchSummary.IsTimedOut;
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
        var searchMatches = _searchMatchList;
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || searchMatches.Length == 0)
        {
            return;
        }

        var nextIndex = GetNextMatchIndex(searchMatches, textEditor.CurrentSelection);
        ApplyMatch(textEditor, searchMatches[nextIndex]);
    }

    public void FindPrevious()
    {
        var searchMatches = _searchMatchList;
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || searchMatches.Length == 0)
        {
            return;
        }

        var previousIndex = GetPreviousMatchIndex(searchMatches, textEditor.CurrentSelection);
        ApplyMatch(textEditor, searchMatches[previousIndex]);
    }

    public void ReplaceCurrent()
    {
        var searchMatches = _searchMatchList;
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || searchMatches.Length == 0)
        {
            return;
        }

        var matchIndex = TryGetExactMatchIndex(searchMatches, textEditor.CurrentSelection);
        if (matchIndex < 0)
        {
            matchIndex = GetNextMatchIndex(searchMatches, textEditor.CurrentSelection);
        }

        var searchMatcher = _searchMatcher;
        if (searchMatcher is null)
        {
            return;
        }

        var documentText = textEditor.Text;
        var match = searchMatches[matchIndex];
        if (!searchMatcher.TryGetReplacementText(documentText, match.ToSearchMatchResult(), ReplaceText, out var replacementText))
        {
            _isDocumentSearchTimedOut = true;
            UpdateSearchStatus();
            return;
        }

        var nextAnchorOffset = match.StartOffset + replacementText.Length;
        textEditor.EditAndReplace(replacementText, match.ToSelection());

        RefreshSearchMatches();

        var refreshedSearchMatches = _searchMatchList;
        if (refreshedSearchMatches.Length == 0)
        {
            textEditor.CurrentCaretOffset = new CaretOffset(nextAnchorOffset);
            return;
        }

        var nextIndex = FindNextMatchIndexFromOffset(refreshedSearchMatches, nextAnchorOffset);
        ApplyMatch(textEditor, refreshedSearchMatches[nextIndex]);
    }

    public void ReplaceAll()
    {
        var searchMatches = _searchMatchList;
        if (IsFolderSearchScope || !TryGetCurrentTextEditor(out var textEditor) || searchMatches.Length == 0)
        {
            return;
        }

        var searchMatcher = _searchMatcher;
        if (searchMatcher is null)
        {
            return;
        }

        var documentText = textEditor.Text;
        for (var i = searchMatches.Length - 1; i >= 0; i--)
        {
            var match = searchMatches[i];
            if (!searchMatcher.TryGetReplacementText(documentText, match.ToSearchMatchResult(), ReplaceText, out var replacementText))
            {
                _isDocumentSearchTimedOut = true;
                UpdateSearchStatus();
                return;
            }

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
            _searchMatchList = [];
            NotifyMatchStateChanged();
            SetSearchStatusText(string.Empty);
            return false;
        }

        return true;
    }

    private void RefreshSearchMatches()
    {
        CancelPendingDocumentSearch();
        _searchMatchList = [];
        _isDocumentSearchTimedOut = false;
        NotifyMatchStateChanged();

        if (IsFolderSearchScope)
        {
            IsSearchingDocument = false;
            UpdateSearchStatus();
            return;
        }

        if (!TryGetCurrentTextEditor(out var textEditor))
        {
            return;
        }

        if (string.IsNullOrEmpty(FindText))
        {
            IsSearchingDocument = false;
            UpdateSearchStatus();
            return;
        }

        var searchMatcher = _searchMatcher;
        if (searchMatcher is null)
        {
            IsSearchingDocument = false;
            UpdateSearchStatus();
            return;
        }

        var documentText = textEditor.Text;
        var searchVersion = ++_documentSearchVersion;
        var cancellationTokenSource = new CancellationTokenSource();
        _documentSearchCancellationTokenSource = cancellationTokenSource;
        IsSearchingDocument = true;
        RunDocumentSearchAsync(searchMatcher, documentText, searchVersion, cancellationTokenSource);
    }

    private void ApplyMatch(TextEditor textEditor, SearchMatch searchMatch)
    {
        textEditor.CurrentSelection = searchMatch.ToSelection();
        UpdateSearchStatus();
    }

    private int GetNextMatchIndex(SearchMatch[] searchMatches, Selection selection)
    {
        var exactMatchIndex = TryGetExactMatchIndex(searchMatches, selection);
        if (exactMatchIndex >= 0)
        {
            return (exactMatchIndex + 1) % searchMatches.Length;
        }

        return FindNextMatchIndexFromOffset(searchMatches, selection.BehindOffset.Offset);
    }

    private int GetPreviousMatchIndex(SearchMatch[] searchMatches, Selection selection)
    {
        var exactMatchIndex = TryGetExactMatchIndex(searchMatches, selection);
        if (exactMatchIndex >= 0)
        {
            return (exactMatchIndex - 1 + searchMatches.Length) % searchMatches.Length;
        }

        return FindPreviousMatchIndexFromOffset(searchMatches, selection.FrontOffset.Offset);
    }

    private int FindNextMatchIndexFromOffset(SearchMatch[] searchMatches, int offset)
    {
        for (var i = 0; i < searchMatches.Length; i++)
        {
            if (searchMatches[i].StartOffset >= offset)
            {
                return i;
            }
        }

        return 0;
    }

    private int FindPreviousMatchIndexFromOffset(SearchMatch[] searchMatches, int offset)
    {
        for (var i = searchMatches.Length - 1; i >= 0; i--)
        {
            if (searchMatches[i].StartOffset < offset)
            {
                return i;
            }
        }

        return searchMatches.Length - 1;
    }

    private int TryGetExactMatchIndex(SearchMatch[] searchMatches, Selection selection)
    {
        for (var i = 0; i < searchMatches.Length; i++)
        {
            if (searchMatches[i].IsMatch(selection))
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

            if (_hasFolderSearchTimedOut)
            {
                SetSearchStatusText("[文件夹查找: 正则匹配超时]");
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

        if (IsSearchingDocument)
        {
            SetSearchStatusText("[查找: 搜索中]");
            return;
        }

        if (_isDocumentSearchTimedOut)
        {
            SetSearchStatusText("[查找: 正则匹配超时]");
            return;
        }

        var searchMatches = _searchMatchList;
        var matchCount = searchMatches.Length;
        if (matchCount == 0)
        {
            SetSearchStatusText("[查找: 0 项]");
            return;
        }

        var currentMatchIndex = -1;
        if (TryGetCurrentTextEditor(out var textEditor))
        {
            currentMatchIndex = TryGetExactMatchIndex(searchMatches, textEditor.CurrentSelection);
            if (currentMatchIndex < 0)
            {
                currentMatchIndex = FindNextMatchIndexFromOffset(searchMatches, textEditor.CurrentSelection.FrontOffset.Offset);
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
        _hasFolderSearchTimedOut = false;
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
        _isDocumentSearchTimedOut = false;
        _hasFolderSearchTimedOut = false;
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

    private async void RunDocumentSearchAsync(SearchMatcher searchMatcher, string documentText, int searchVersion, CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            var searchExecutionResult = await searchMatcher.FindMatchesAsync(documentText, cancellationTokenSource.Token);
            if (cancellationTokenSource.IsCancellationRequested || searchVersion != _documentSearchVersion)
            {
                return;
            }

            _isDocumentSearchTimedOut = searchExecutionResult.IsTimedOut;
            _searchMatchList = searchExecutionResult.MatchList
                .Select(searchMatch => new SearchMatch(searchMatch.StartOffset, searchMatch.Length, searchMatch.Value))
                .ToArray();

            NotifyMatchStateChanged();
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_documentSearchCancellationTokenSource, cancellationTokenSource))
            {
                _documentSearchCancellationTokenSource = null;
                IsSearchingDocument = false;
                NotifyMatchStateChanged();
                UpdateSearchStatus();
            }

            cancellationTokenSource.Dispose();
        }
    }

    private void CancelPendingDocumentSearch()
    {
        var cancellationTokenSource = _documentSearchCancellationTokenSource;
        if (cancellationTokenSource is null)
        {
            return;
        }

        _documentSearchCancellationTokenSource = null;
        cancellationTokenSource.Cancel();
    }

    private volatile SearchMatch[] _searchMatchList = [];
    private TextEditor? _currentTextEditor;
    private CancellationTokenSource? _documentSearchCancellationTokenSource;
    private int _documentSearchVersion;
    private bool _isPanelVisible;
    private bool _isReplaceMode;
    private bool _isFolderSearchScope;
    private bool _isCaseInsensitive;
    private bool _isSearchingDocument;
    private bool _isDocumentSearchTimedOut;
    private bool _isSearchingFolder;
    private bool _hasFolderSearchTimedOut;
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
