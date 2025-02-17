using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.DocumentEventArgs;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Document.Utils;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document
{
    /// <summary>
    /// 提供文档管理，只提供数据管理，这里属于更高级的 API 层，将提供更多的细节控制
    /// </summary>
    public class DocumentManager
    {
        /// <inheritdoc cref="T:LightTextEditorPlus.Core.Document.DocumentManager"/>
        public DocumentManager(TextEditorCore textEditor)
        {
            TextEditor = textEditor;
            IReadOnlyRunProperty styleRunProperty = textEditor.PlatformProvider.GetPlatformRunPropertyCreator().GetDefaultRunProperty();
            _styleRunProperty = styleRunProperty;
            StyleParagraphProperty = new ParagraphProperty();
            
            ParagraphManager = new ParagraphManager(textEditor);
            DocumentRunEditProvider = new DocumentRunEditProvider(textEditor);
        }

        #region 框架

        internal TextEditorCore TextEditor { get; }

        #region DocumentWidth DocumentHeight

        /// <summary>
        /// 文档的宽度。受 <see cref="LightTextEditorPlus.Core.TextEditorCore.SizeToContent"/> 影响
        /// </summary>
        /// 文档的宽度不等于渲染宽度。布局尺寸请参阅 <see cref="TextEditorCore.GetDocumentLayoutBounds"/> 方法
        public double DocumentWidth
        {
            set
            {
                if (Nearly.Equals(_documentWidth, value))
                {
                    return;
                }

                _documentWidth = value;

                TextEditor.RequireDispatchReLayoutAllDocument("DocumentWidthChanged");
            }
            get => _documentWidth;
        }

        private double _documentWidth = double.PositiveInfinity;

        /// <summary>
        /// 文档的高度。受 <see cref="LightTextEditorPlus.Core.TextEditorCore.SizeToContent"/> 影响
        /// </summary>
        /// 文档的高度不等于渲染高度。布局尺寸请参阅 <see cref="TextEditorCore.GetDocumentLayoutBounds"/> 方法
        public double DocumentHeight
        {
            set
            {
                if (Nearly.Equals(_documentHeight, value))
                {
                    return;
                }

                _documentHeight = value;

                TextEditor.RequireDispatchReLayoutAllDocument("DocumentHeightChanged");
            }
            get => _documentHeight;
        }

        private double _documentHeight = double.PositiveInfinity;

        // todo 考虑添加最大文档宽度高度的支持
        //private double MaxDocumentWidth { get; set; }

        #endregion

        /// <summary>
        /// 文档的字符编辑提供器
        /// </summary>
        /// 和 <see cref="ParagraphManager"/> 不同的是，此属性用来辅助处理字符编辑。而 <see cref="ParagraphManager"/> 用来修改段落
        private DocumentRunEditProvider DocumentRunEditProvider { get; }

        /// <summary>
        /// 段落管理。这是存放所有的字符的属性。字符存放在段落里面
        /// </summary>
        internal ParagraphManager ParagraphManager { get; }

        /// <summary>
        /// 管理光标
        /// </summary>
        private CaretManager CaretManager => TextEditor.CaretManager;

        #region 事件

        ///// <summary>
        ///// 给内部提供的文档尺寸变更事件
        ///// </summary>
        //internal event EventHandler? InternalDocumentSizeChanged; 

        /// <summary>
        /// 给内部提供的文档开始变更事件
        /// </summary>
        internal event EventHandler<DocumentChangeEventArgs>? InternalDocumentChanging;

        /// <summary>
        /// 给内部提供的文档变更完成事件
        /// </summary>
        internal event EventHandler<DocumentChangeEventArgs>? InternalDocumentChanged;

        #endregion

        #region Paragraph段落

        /// <summary>
        /// 设置或获取文本的样式段落属性。此属性一般只在初始化时设置和使用，当首段文本创建时，将会继承此样式。但当文本已经完成初始化之后，即存在过文本之后，则此属性设置将对文本毫无影响
        /// <para>
        /// 此属性的存在只有两个功能：
        /// 1. 作为文本初始化的默认样式
        /// 2. 给业务方获取样式段落属性方便使用 with 关键字创建出新的段落属性
        /// </para>
        /// </summary>
        public ParagraphProperty StyleParagraphProperty
        {
            get;
            private set;
            //[MemberNotNull(nameof(_initParagraphProperty))]
            //set
            //{
            //    var isInit = ParagraphManager.GetRawParagraphList().Count == 0;
            //    if (!isInit && !TextEditor.IsUndoRedoMode)
            //    {
            //        string message = "禁止在初始化完成之后，即存在段落之后，设置默认的段落属性";
            //        if (TextEditor.IsInDebugMode)
            //        {
            //            throw new TextEditorDebugException(message);
            //        }
            //        else
            //        {
            //            TextEditor.Logger.LogWarning(message);
            //        }
            //    }

            //    _initParagraphProperty = value;
            //}
            //get
            //{
            // 这个逻辑太复杂了，取的是首段的段落属性
            //IReadOnlyList<ParagraphData> paragraphList = ParagraphManager.GetRawParagraphList();
            //int count = paragraphList.Count;
            //if (count == 0)
            //{
            //    // 没有段落，那就使用默认段落属性
            //    return _initParagraphProperty;
            //}
            //else
            //{
            //    return paragraphList[0].ParagraphProperty;
            //}
            //}
        }

        ///// <summary>
        ///// 当前光标下的段落
        ///// </summary>
        //internal ParagraphData CurrentCaretParagraphData
        //{
        //    get
        //    {
        //        var hitParagraphDataResult = ParagraphManager.GetHitParagraphData(CaretManager.CurrentCaretOffset);
        //        var paragraphData = hitParagraphDataResult.ParagraphData;
        //        return paragraphData;
        //    }
        //}

        /// <summary>
        /// 设置当前文本的样式段落属性
        /// </summary>
        /// <remarks>
        /// 仅当文本没有创建出任何段落之前，初始化过程中，才能设置文本的样式段落属性
        /// </remarks>
        /// <exception cref="InvalidOperationException">如果文本已经创建出任何段落或完成任何初始化，则抛出此异常。请确保此方法在刚创建出文本时立刻调用。否则则请设置对应段落的样式</exception>
        public void SetStyleParagraphProperty(ParagraphProperty paragraphProperty)
        {
            var isInit = IsInitializingTextEditor();
            if (!isInit)
            {
                throw new InvalidOperationException($"仅当文本没有创建出任何段落之前，初始化过程中，才能设置文本的样式字符属性");
            }

            StyleParagraphProperty = paragraphProperty;
        }

        /// <summary>
        /// 设置段落属性
        /// </summary>
        /// <param name="paragraphIndex"></param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(ParagraphIndex paragraphIndex, ParagraphProperty paragraphProperty)
        {
            ParagraphData paragraphData = ParagraphManager.GetParagraph(paragraphIndex);
            SetParagraphProperty(paragraphData, paragraphProperty);
        }

        /// <summary>
        /// 设置 <paramref name="caretOffset"/> 光标所在的段落的段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(in CaretOffset caretOffset, ParagraphProperty paragraphProperty)
        {
            ParagraphData paragraphData = ParagraphManager.GetHitParagraphData(caretOffset).ParagraphData;
            SetParagraphProperty(paragraphData, paragraphProperty);
        }

        private void SetParagraphProperty(ParagraphData paragraphData, ParagraphProperty paragraphProperty)
        {
            paragraphProperty.Verify();

            if (TextEditor.ShouldInsertUndoRedo)
            {
                // 加入撤销重做
                var oldValue = paragraphData.ParagraphProperty;
                var operation = new ParagraphPropertyChangeOperation(TextEditor, oldValue, paragraphProperty, paragraphData.Index);
                TextEditor.UndoRedoProvider.Insert(operation);
            }

            // todo 考虑带项目符号时，需要更多更多的范围
            // 例如当前文本是如下内容：
            // 1. 
            // 2.
            // 3.
            // 然后将 2. 的段落修改为其他项目符号，此时需要更新 3. 段落
            InternalDocumentChanging?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.OnlyStyle));
            paragraphData.SetParagraphProperty(paragraphProperty);
            InternalDocumentChanged?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.OnlyStyle));
        }

        /// <summary>
        /// 获取段落属性
        /// </summary>
        public ParagraphProperty GetParagraphProperty(ParagraphIndex paragraphIndex)
        {
            return ParagraphManager.GetParagraph(paragraphIndex).ParagraphProperty;
        }

        /// <summary>
        /// 获取段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <returns></returns>
        public ParagraphProperty GetParagraphProperty(in CaretOffset caretOffset)
            => ParagraphManager.GetHitParagraphData(caretOffset).ParagraphData.ParagraphProperty;

        #endregion

        #region RunProperty

        /// <summary>
        /// 获取当前文本的样式字符属性。样式字符属性只用来作为初始化文本的默认字符属性，以及方便业务方使用 with 关键字创建新的字符属性
        /// <para>
        /// 此属性在文本初始化之后再次设置将不会影响任何文本内容，即存在过文本之后，则此属性设置将对文本毫无影响
        /// </para>
        /// </summary>
        public IReadOnlyRunProperty StyleRunProperty
        {
            private set
            {
                if (_styleRunProperty == value)
                {
                    return;
                }

                var oldValue = _styleRunProperty;

                _styleRunProperty = value;

                if (TextEditor.ShouldInsertUndoRedo)
                {
                    // 如果应该记录撤销重做，
                    // 如不在撤销恢复模式，且开启了撤销恢复功能
                    // 那就记录一条
                    var operation =
                        new ChangeTextEditorStyleTextRunPropertyValueOperation(TextEditor, value, oldValue);
                    TextEditor.InsertUndoRedoOperation(operation);
                }
            }
            get => _styleRunProperty;
        }

        private IReadOnlyRunProperty _styleRunProperty;

        /// <inheritdoc cref="P:LightTextEditorPlus.Core.Carets.CaretManager.CurrentCaretRunProperty"/>
        public IReadOnlyRunProperty CurrentCaretRunProperty
        {
            //private set => CaretManager.CurrentCaretRunProperty = value;
            //get => GetCurrentCaretRunProperty();
            get
            {
                // 获取当前光标的字符属性
                // 规则：
                //
                // 有 CaretManager.CurrentCaretRunProperty 时，返回此属性
                // 无任何文本字符时，获取段落和文档的属性
                // 有字符时，非段首则获取字符前一个字符的属性；段首则获取段落的字符属性
                if (CaretManager.CurrentCaretRunProperty is not null)
                {
                    // 有 CaretManager.CurrentCaretRunProperty 时，返回此属性
                    return CaretManager.CurrentCaretRunProperty;
                }

                IReadOnlyRunProperty currentCaretRunProperty;
                // 无文本时，也是有第一段的。因此不能通过 CharCount == 0 判断空文本
                // 而是应该通过 IsInitializingTextEditor 判断是否正在初始化，即连第一段可能都没有的情况
                //if (CharCount == 0)
                if (IsInitializingTextEditor())
                {
                    // 如果当前正在初始化过程，则获取样式字符属性
                    currentCaretRunProperty = StyleRunProperty;
                }
                else
                {
                    var hitParagraphDataResult = ParagraphManager.GetHitParagraphData(CaretManager.CurrentCaretOffset);
                    var paragraphData = hitParagraphDataResult.ParagraphData;
                    // 为了复用 HitParagraphDataResult 内容，不调用 CurrentCaretParagraphData 属性
                    //paragraphData = CurrentCaretParagraphData;

                    // 当前光标是否在段首
                    if (hitParagraphDataResult.HitOffset.Offset == 0)
                    {
                        // 如果是在段首（当前只判断是文档开头）
                        // 则取此光标之后一个字符的，如果光标之后没有字符了，那只能使用默认的
                        if (paragraphData.CharCount > 0)
                        {
                            // 取段落首个字符的字符属性
                            var charData = paragraphData.GetCharData(new ParagraphCharOffset(0));
                            currentCaretRunProperty = charData.RunProperty;
                        }
                        else
                        {
                            // 这个段落没有字符，那就使用段落默认字符属性
                            currentCaretRunProperty =
                                paragraphData.ParagraphStartRunProperty;
                        }
                    }
                    else
                    {
                        // 不在段首，那就取光标前一个字符的文本属性
                        // 取前面一个字符
                        int hitOffsetOffset = hitParagraphDataResult.HitOffset.Offset - 1;
                        var paragraphCharOffset = new ParagraphCharOffset(hitOffsetOffset);
                        var charData = paragraphData.GetCharData(paragraphCharOffset);
                        currentCaretRunProperty = charData.RunProperty;
                    }
                }

                return currentCaretRunProperty;
            }
        }

        #endregion

        #endregion

        #region 公开属性

        /// <summary>
        /// 文档的字符数量。段落之间，使用换行符，加入计算为换行符字符。不包含项目符号
        /// </summary>
        public int CharCount
        {
            get
            {
                IReadOnlyList<ParagraphData> rawParagraphList = DocumentRunEditProvider.ParagraphManager.GetRawParagraphList();
                // 为什么不调用 DocumentRunEditProvider.ParagraphManager.GetParagraphList() 方法？因为担心 GetParagraphList 额外调用了确保至少一段的方法，导致不必要的损耗
                if (rawParagraphList.Count == 0)
                {
                    return 0;
                }

                var sum = 0;
                foreach (var paragraphData in rawParagraphList)
                {
                    sum += paragraphData.CharCount;
                    // 加上换行符的字符
                    sum += ParagraphData.DelimiterLength;
                }

                if (sum > 0)
                {
                    // 证明存在一段以上，那减去最后一段多加上的换行符
                    sum -= ParagraphData.DelimiterLength;
                }

                return sum;
            }
        }

        #endregion

        #region 公开方法

        #region RunProperty

        /// <summary>
        /// 设置当前文本的样式字符属性
        /// </summary>
        /// <remarks>
        /// 仅当文本没有创建出任何段落之前，初始化过程中，才能设置文本的样式字符属性
        /// </remarks>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        public void SetStyleTextRunProperty<T>(ConfigReadOnlyRunProperty<T> config) where T : IReadOnlyRunProperty
        {
            var isInit = IsInitializingTextEditor();
            if (!isInit)
            {
                throw new InvalidOperationException($"仅当文本没有创建出任何段落之前，初始化过程中，才能设置文本的样式字符属性");
            }

            T runProperty = config((T) StyleRunProperty);
            StyleRunProperty = runProperty;
        }

        /// <summary>
        /// 是否正在初始化文本编辑器
        /// </summary>
        /// <returns></returns>
        /// 如果文本编辑器有过一次文本，则段落列表不为空，那就不是初始化状态。这和是否空文本是不同的
        private bool IsInitializingTextEditor() => ParagraphManager.GetRawParagraphList().Count == 0;

        /// <summary>
        /// 设置当前光标的字符属性。在光标切走之后，自动失效
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        /// <param name="config"></param>
        public void SetCurrentCaretRunProperty<T>(ConfigReadOnlyRunProperty<T> config) where T : IReadOnlyRunProperty
        {
            // 先获取当前光标的字符属性吧
            IReadOnlyRunProperty currentCaretRunProperty = CurrentCaretRunProperty;

            CaretManager.CurrentCaretRunProperty = config((T) currentCaretRunProperty);
        }

        /// <summary>
        /// 将设置一段范围内的文本的字符属性。如传入的 <paramref name="selection"/> 为空，且当前也没有选择内容，则仅修改当前光标的字符属性
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        /// <param name="config"></param>
        /// <param name="selection">如为空，则采用当前选择内容。如当前也没有选择内容，则仅修改当前光标的字符属性</param>
        public void SetRunProperty<T>(ConfigReadOnlyRunProperty<T> config, Selection? selection)
            where T : IReadOnlyRunProperty
        {
            // 如为空，则采用当前选择内容
            selection ??= CaretManager.CurrentSelection;

            // 如当前也没有选择内容，则仅修改当前光标的字符属性
            if (selection.Value.IsEmpty)
            {
                // 设置当前的光标样式，没有修改文档内容，不需要触发文档变更事件
                if (selection.Value.FrontOffset != CaretManager.CurrentCaretOffset)
                {
                    // 这是在搞什么呀？对一个没有选择内容的地方设置文本字符属性
                    // 这里也不合适抛出异常，可以忽略
                    // 文本库允许你这么做，但是这么做，文本库啥都不干
                    TextEditor.Logger.LogDebug($"[DocumentManager][SetRunProperty] selection is empty, but not equals CurrentCaretOffset. 传入 selection 范围的长度是 0 且起点不等于当前光标坐标。将不会修改任何文本字符属性 selection.FrontOffset={selection.Value.FrontOffset};CaretManager.CurrentCaretOffset={CaretManager.CurrentCaretOffset}");
                }
                else
                {
                    SetCurrentCaretRunProperty(config);
                }
            }
            else
            {
                // 修改属性，需要触发样式变更，也就是文档变更
                InternalDocumentChanging?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.OnlyStyle));
                // 表示最后一个更改之后的文本字符属性，为了提升性能，不让每个文本字符属性都需要执行 config 函数
                // 用来判断如果相邻两个字符的字符属性是相同的，就可以直接复用，不需要重新执行 config 函数创建新的字符属性对象
                IReadOnlyRunProperty? lastChangedRunProperty = null;
                CharData? lastCharData = null;

                var runList = new ImmutableRunList();

                foreach (var charData in GetCharDataRange(selection.Value))
                {
                    Debug.Assert(charData.CharLayoutData != null, "能够从段落里获取到的，一定是存在在段落里面，因此此属性一定不为空");
                    
                    IReadOnlyRunProperty currentRunProperty;

                    if (ReferenceEquals(charData.RunProperty, lastCharData?.RunProperty))
                    {
                        // 如果相邻两个 CharData 采用相同的字符属性，那就不需要再创建了，稍微提升一点性能和减少内存占用
                        Debug.Assert(lastChangedRunProperty != null, "当前字符和上一个字符的字符属性相同，证明存在上一个字符，证明存在上一个字符属性");
                        // ReSharper disable once RedundantSuppressNullableWarningExpression
                        currentRunProperty = lastChangedRunProperty!;
                    }
                    else
                    {
                        currentRunProperty = charData.RunProperty;

                        currentRunProperty = config((T) currentRunProperty);
                    }

                    if (charData.IsLineBreakCharData)
                    {
                        // 是换行的话，需要加上换行的字符
                        runList.Add(new LineBreakRun(currentRunProperty));
                    }
                    else
                    {
                        runList.Add(new SingleCharImmutableRun(charData.CharObject, currentRunProperty));
                    }

                    lastChangedRunProperty = currentRunProperty;
                    lastCharData = charData;
                }

                ReplaceCore(selection.Value, runList);

                // 只触发文档变更，不需要修改光标

                InternalDocumentChanged?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.OnlyStyle));
            }
        }

        /// <summary>
        /// 是否给定范围内的字符属性都满足 <paramref name="predicate"/> 条件。如传入的 <paramref name="selection"/> 为空，且当前也没有选择内容，则仅判断当前光标的字符属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <param name="selection"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool AreAllRunPropertiesMatch<T>(Predicate<T> predicate, Selection? selection = null) where T : IReadOnlyRunProperty
        {
            selection ??= CaretManager.CurrentSelection;
            if (selection.Value.IsEmpty)
            {
                // 获取当前光标的属性即可
                return predicate((T) CurrentCaretRunProperty);
            }
            else
            {
                foreach (var runProperty in GetDifferentRunPropertyRange(selection.Value))
                {
                    if (predicate((T) runProperty))
                    {
                        // 如果满足条件，那就继续判断下一个
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #endregion

        /// <summary>
        /// 获取给定的 <paramref name="selection"/> 范围有多少不连续相同的字符属性
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        internal IEnumerable<IReadOnlyRunProperty> GetDifferentRunPropertyRange(in Selection selection)
        {
            var runPropertyRange = GetRunPropertyRange(selection);

            return runPropertyRange.GetDifferentRunPropertyRangeInner();
        }

        /// <summary>
        /// 获取给定范围的字符属性
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public IEnumerable<IReadOnlyRunProperty> GetRunPropertyRange(in Selection selection)
        {
            return GetCharDataRange(selection).Select(t => t.RunProperty);
        }

        /// <summary>
        /// 给定选择范围内的所有字符属性
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        internal IEnumerable<CharData> GetCharDataRange(in Selection selection)
        {
            // 获取方法：
            // 1. 先获取命中到的首段，取首段的字符
            // 2. 如果首段不够，则获取后续段落，每个段落获取之前，都添加用来表示换行的字符
            if (selection.IsEmpty)
            {
                return Enumerable.Empty<CharData>();
            }

            var result = ParagraphManager.GetHitParagraphData(selection.FrontOffset);
            var remainingLength = selection.Length;

            var takeCount = Math.Min(result.ParagraphData.CharCount - result.HitOffset.Offset, remainingLength);

            var charDataList = result.ParagraphData.ToReadOnlyListSpan(new ParagraphCharOffset(result.HitOffset.Offset),
                takeCount);
            remainingLength -= takeCount;
            IEnumerable<CharData> charDataListResult = charDataList;

            // 继续获取后续段落，如果首段不够的话
            var lastParagraphData = result.ParagraphData;
            var list = ParagraphManager.GetParagraphList();
            for (int i = result.ParagraphData.Index.Index + 1; i < list.Count && remainingLength > 0; i++)
            {
                // 加上段末换行符
                remainingLength -= ParagraphData.DelimiterLength;
                charDataListResult =
                    charDataListResult.Concat(new[] { lastParagraphData.GetLineBreakCharData() });

                var currentParagraphData = list[i];
                takeCount = Math.Min(currentParagraphData.CharCount, remainingLength);
                charDataListResult =
                    charDataListResult.Concat(currentParagraphData.ToReadOnlyListSpan(new ParagraphCharOffset(0), takeCount));
                remainingLength -= takeCount;
                lastParagraphData = currentParagraphData;
            }

            return charDataListResult;
        }

        /// <summary>
        /// 获取不可变的文本块列表。如考虑性能，请优先选择 <see cref="GetCharDataRange"/> 方法或 <see cref="EnumerateImmutableRunRange"/> 方法
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public IImmutableRunList GetImmutableRunList(in Selection selection)
        {
            IEnumerable<IImmutableRun> enumerateImmutableRunRange = EnumerateImmutableRunRange(selection);
            return new ImmutableRunList(enumerateImmutableRunRange);
        }

        /// <summary>
        /// 枚举给定范围的不可变文本块
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public IEnumerable<IImmutableRun> EnumerateImmutableRunRange(in Selection selection) =>
            this.GetImmutableRunRangeInner(selection);

        #region 编辑

        /// <summary>
        /// 追加一段文本
        /// </summary>
        /// 由于追加属于性能优化的高级用法，默认不开放给业务层调用
        internal void AppendText(IImmutableRun run)
        {
            TextEditor.AddLayoutReason(nameof(AppendText));

            InternalDocumentChanging?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.Text));

            // 设置光标到文档最后，再进行追加。设置光标到文档最后之后，可以自动获取当前光标下的文本字符属性
            var oldCharCount = CharCount;
            CaretManager.CurrentCaretOffset = new CaretOffset(oldCharCount);

            DocumentRunEditProvider.Append(run);

            var newCharCount = CharCount;
            var newCaretOffsetIsAtLineStart = ImmutableRunHelper.IsEndWithBreakLine(run);
            CaretOffset newCaretOffset = new CaretOffset(newCharCount, newCaretOffsetIsAtLineStart);
            CaretManager.CurrentCaretOffset = newCaretOffset;

            if (TextEditor.ShouldInsertUndoRedo)
            {
                // 如果最后一段是空段，则证明命中到段首
                var lastParagraph = ParagraphManager.GetLastParagraph();
                var isAtLineStart = lastParagraph.IsEmptyParagraph;

                CaretOffset oldCaretOffset = new CaretOffset(oldCharCount, isAtLineStart);
                var oldSelection = new Selection(oldCaretOffset, length: 0);
                IImmutableRunList? oldRun = null; // 追加的过程，是没有替换的，即 oldRun 一定是空
                var newSelection = new Selection(oldCaretOffset, newCaretOffset);

                // 不能直接使用 run 的内容，因为 run 里可能没有写好使用的样式。因此需要获取实际插入的内容，从而获取到实际的插入带样式文本
                var newRun = GetImmutableRunList(newSelection);
                var textChangeOperation = new TextChangeOperation(TextEditor, oldSelection, oldRun, newSelection, newRun);
                TextEditor.UndoRedoProvider.Insert(textChangeOperation);
            }

            InternalDocumentChanged?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.Text));
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="run">传入 null 表示删除 <paramref name="selection"/> 范围文本</param>
        internal void EditAndReplaceRun(in Selection selection, IImmutableRun? run)
        {
            TextEditor.AddLayoutReason("DocumentManager.EditAndReplaceRun");

            EditAndReplaceRunInner(selection, run);
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        private void EditAndReplaceRunInner(in Selection selection, IImmutableRun? run)
        {
            if (run is TextRun textRun)
            {
                // 特别处理 TextRun 情况，仅仅只是为了提升几乎可以忽略的性能
                EditAndReplaceRunListInner(selection, textRun);
            }
            else if (run is null)
            {
                // 传入 null 的意思就是删除选择范围
                EditAndReplaceRunListInner(selection, null);
            }
            else
            {
                EditAndReplaceRunListInner(selection, new SingleImmutableRunList(run));
            }
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="run"></param>
        internal void EditAndReplaceRunList(in Selection selection, IImmutableRunList? run)
        {
            TextEditor.AddLayoutReason("DocumentManager.EditAndReplaceRunList");

            EditAndReplaceRunListInner(selection, run);
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        private void EditAndReplaceRunListInner(in Selection selection, IImmutableRunList? run)
        {
            InternalDocumentChanging?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.Text));
            // 这里只处理数据变更，后续渲染需要通过 InternalDocumentChanged 事件触发

            // 替换文本
            ReplaceCore(selection, run);

            // 修改光标
            var addCharCount = run?.CharCount ?? 0;
            var isAtLineStart = selection.FrontOffset.IsAtLineStart;
            if (addCharCount > 0)
            {
                // 有添加内容，则不在行首
                isAtLineStart = false;
            }
            var caretOffset = selection.FrontOffset.Offset + addCharCount;
            // 是否更改了文本内容。也就是有添加或者有删除。有添加则 addCharCount != 0 成立。有删除则 selection.Length > 0 成立
            var isChangedText = addCharCount != 0 || selection.Length > 0;
            if (isChangedText)
            {
                // 如果有添加内容，则需要判断是否采用换行符结束，如果采用换行符结束，需要设置光标在行首。一旦 IsEndWithBreakLine 为 true 的值，则原本的 isAtLineStart 必定是 false 值
                Debug.Assert((isAtLineStart && ImmutableRunHelper.IsEndWithBreakLine(run)) == false, "一旦 IsEndWithBreakLine 为 true 的值，则原本的 isAtLineStart 必定是 false 值。不可能两个同时为 true 的值");
                isAtLineStart = isAtLineStart || ImmutableRunHelper.IsEndWithBreakLine(run);

                CaretOffset currentCaretOffset = new CaretOffset(caretOffset, isAtLineStart);

                if (CaretManager.CurrentCaretOffset.Offset != caretOffset)
                {
                    // 如果有加上任何内容，则需要判断是否采用换行符结束，如果采用换行符结束，需要设置光标是在行首
                    // 即在 IsEndWithBreakLine(run) 已经处理过了
                    // 如果仅仅只是替换相等同的内容，如 CaretManager.CurrentCaretOffset.Offset == caretOffset.Offset 的条件，则不应该修改光标。这条规则也许不对，如果后续行为不符合交互设计，则进行修改

                    // 这个逻辑是错误的，需要带上退格方向才能确定光标位置
                    //// 如果没有加上任何内容，即进入删除的情况。则需要判断删除之后的光标是否落在段首
                    //if (run is null 
                    //    // 原本不在段首。 如果原本就在段首，则可能是删除空段
                    //    && !CaretManager.CurrentCaretOffset.IsAtLineStart)
                    //{
                    //    // 尝试命中一下。如果此时刚好命中到上一段的段末，则应该修正为下一段的段首
                    //    HitParagraphDataResult hitParagraphDataResult = ParagraphManager.GetHitParagraphData(currentCaretOffset);
                    //    // 修正规则：原本不在段首，但是删除之后在上一段的段末，则修正为段首
                    //}

                    CaretManager.CurrentCaretOffset = currentCaretOffset;
                }
                else
                {
                    if (CaretManager.CurrentSelection.Length > 0)
                    {
                        // 现在是选中删除的情况，应该将光标设置到删除的位置
                        // 通过给 CurrentCaretOffset 赋值的方式清空选择内容
                        CaretManager.CurrentCaretOffset = currentCaretOffset;
                    }
                }
            }
            else
            {
            }

            // 触发事件。触发事件将用来执行重新排版
            InternalDocumentChanged?.Invoke(this, new DocumentChangeEventArgs(DocumentChangeKind.Text));
        }
        
        private void ReplaceCore(in Selection selection, IImmutableRunList? run)
        {
            if (selection.BehindOffset.Offset > CharCount)
            {
                throw new SelectionOutOfRangeException(TextEditor, selection, CharCount);
            }

            if (TextEditor.ShouldInsertUndoRedo)
            {
                // 需要插入撤销恢复，先获取旧的数据，再替换，再获取新的数据
                var oldSelection = selection;
                // 获取旧的数据
                IImmutableRunList oldList = GetImmutableRunList(oldSelection);

                // 执行替换，需要替换之后才能获取到新的数据
                DocumentRunEditProvider.Replace(selection, run);

                // 获取新的数据
                var newSelection = new Selection(selection.FrontOffset, run?.CharCount ?? 0);
                var newList = GetImmutableRunList(newSelection);

                var textChangeOperation = new TextChangeOperation(TextEditor, oldSelection, oldList, newSelection, newList);
                TextEditor.UndoRedoProvider.Insert(textChangeOperation);
            }
            else
            {
                // 不需要插入撤销恢复，那就直接替换
                DocumentRunEditProvider.Replace(selection, run);
            }
        }

        #endregion

        #region 删除

        /// <summary>
        /// 退格删除
        /// </summary>
        /// <param name="count"></param>
        internal void Backspace(int count = 1)
        {
            // 退格键时，有选择就删除选择内容。没选择就删除给定内容
            var caretManager = CaretManager;
            var currentSelection = caretManager.CurrentSelection;
            var removedSelection = currentSelection;
            if (currentSelection.IsEmpty)
            {
                var currentCaretOffset = caretManager.CurrentCaretOffset;
                if (currentCaretOffset.Offset == 0)
                {
                    // 放在文档最前，不能退格
                    return;
                }

                var offset = currentCaretOffset.Offset - count;
                offset = Math.Max(0, offset);
                var length = currentCaretOffset.Offset - offset;

                // 判断删除之后是否在行首的情况
                // 尝试命中一下段落。如果此时刚好命中到上一段的段末，则应该修正为下一段的段首
                var testCaretOffset = new CaretOffset(offset);
                HitParagraphDataResult hitParagraphDataResult = ParagraphManager.GetHitParagraphData(testCaretOffset);
                var isAtLineStart = hitParagraphDataResult.HitOffset.Offset == 0;
                removedSelection = new Selection(new CaretOffset(offset, isAtLineStart), length);
            }

            TextEditor.AddLayoutReason(nameof(Backspace) + "退格删除");
            RemoveInner(removedSelection);
        }

        /// <summary>
        /// 帝利特删除
        /// </summary>
        /// <param name="count"></param>
        internal void Delete(int count = 1)
        {
            // 有选择就删除选择内容。没选择就删除光标之后的内容
            var caretManager = CaretManager;
            var currentSelection = caretManager.CurrentSelection;
            if (currentSelection.IsEmpty)
            {
                var currentCaretOffset = caretManager.CurrentCaretOffset;
                var charCount = CharCount;
                if (currentCaretOffset.Offset == charCount)
                {
                    // 光标在文档最后，不能使用帝利特删除
                    return;
                }

                // 获取可以删除的字符数量
                var remainCount = charCount - currentCaretOffset.Offset;
                var deleteCount = Math.Min(count, remainCount);
                currentSelection = new Selection(currentCaretOffset, deleteCount);
            }

            TextEditor.AddLayoutReason(nameof(Delete) + "帝利特删除");
            RemoveInner(currentSelection);
        }

        internal void Remove(in Selection selection)
        {
            if (selection.IsEmpty)
            {
                return;
            }

            TextEditor.AddLayoutReason(nameof(Remove) + "删除范围文本");
            RemoveInner(selection);
        }

        private void RemoveInner(in Selection selection)
        {
            // 删除范围内的文本，等价于将范围内的文本替换为空
            EditAndReplaceRunInner(selection, null);
        }

        #endregion

        #endregion

        #region UndoRedo

        // 这里的方法只允许撤销恢复调用

        /// <summary>
        /// 从撤销重做里面设置回默认的文本字符属性
        /// </summary>
        /// <param name="runProperty"></param>
        internal void SetStyleTextRunPropertyByUndoRedo(IReadOnlyRunProperty runProperty)
        {
            TextEditor.VerifyInUndoRedoMode();
            StyleRunProperty = runProperty;
        }

        #endregion
    }
}
