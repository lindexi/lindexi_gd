using System;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Utils;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Document
{
    /// <summary>
    /// 提供文档管理，只提供数据管理，这里属于更高级的 API 层，将提供更多的细节控制
    /// </summary>
    public class DocumentManager
    {
        public DocumentManager(TextEditor textEditor)
        {
            TextEditor = textEditor;
            CurrentParagraphProperty = new ParagraphProperty();
            _currentRunProperty = textEditor.PlatformRunPropertyCreator.GetDefaultRunProperty();

            ParagraphManager = new ParagraphManager(textEditor);
            DocumentRunEditProvider = new DocumentRunEditProvider(textEditor);
        }

        #region 框架

        public TextEditor TextEditor { get; }

        /// <summary>
        /// 文档的宽度。受 <see cref="LightTextEditorPlus.Core.TextEditorCore.SizeToContent"/> 影响
        /// </summary>
        /// 文档的宽度不等于渲染宽度。布局尺寸请参阅 <see cref="TextEditorCore.GetDocumentLayoutBounds"/> 方法
        public double DocumentWidth
        {
            set
            {
                _documentWidth = value;

                TextEditor.RequireDispatchUpdateLayout("DocumentWidthChanged");
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
                _documentHeight = value;

                TextEditor.RequireDispatchUpdateLayout("DocumentHeightChanged");
            }
            get => _documentHeight;
        }

        private double _documentHeight = double.PositiveInfinity;

        /// <summary>
        /// 文档的字符编辑提供器
        /// </summary>
        internal DocumentRunEditProvider DocumentRunEditProvider { get; }

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
        internal event EventHandler? InternalDocumentChanging;

        /// <summary>
        /// 给内部提供的文档变更完成事件
        /// </summary>
        internal event EventHandler? InternalDocumentChanged;

        #endregion

        /// <summary>
        /// 设置或获取当前文本的默认段落属性。设置之后，只影响新变更的文本，不影响之前的文本
        /// </summary>
        public ParagraphProperty CurrentParagraphProperty { set; get; }

        /// <summary>
        /// 获取当前文本的默认字符属性
        /// </summary>
        public IReadOnlyRunProperty CurrentRunProperty
        {
            private set
            {
                var oldValue = _currentRunProperty;
                _currentRunProperty = value;

                if (!TextEditor.IsUndoRedoMode)
                {
                    // 如果不在撤销恢复模式，那就记录一条
                    var operation =
                        new ChangeTextEditorDefaultTextRunPropertyValueOperation(TextEditor, value, oldValue);
                    TextEditor.InsertUndoRedoOperation(operation);
                }
            }
            get => _currentRunProperty;
        }

        private IReadOnlyRunProperty _currentRunProperty;

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
                if (CharCount == 0)
                {
                    // 无任何文本字符时，获取段落和文档的属性
                    currentCaretRunProperty = CurrentRunProperty;
                }
                else
                {
                    var hitParagraphDataResult = ParagraphManager.GetHitParagraphData(CaretManager.CurrentCaretOffset);
                    var paragraphData = hitParagraphDataResult.ParagraphData;

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
                            // 这个段落没有字符，那就使用段落默认字符属性，段落没有默认的字符属性，那就采用文档属性
                            currentCaretRunProperty =
                                paragraphData.ParagraphProperty.ParagraphStartRunProperty ??
                                CurrentRunProperty;
                        }
                    }
                    else
                    {
                        // 不在段首，那就取光标前一个字符的文本属性
                        var paragraphCharOffset = new ParagraphCharOffset(hitParagraphDataResult.HitOffset.Offset - 1);
                        var charData = paragraphData.GetCharData(paragraphCharOffset);
                        currentCaretRunProperty = charData.RunProperty;
                    }
                }
                return currentCaretRunProperty;
            }
        }

        //private IReadOnlyRunProperty GetCurrentCaretRunProperty()
        //{

        //}

        #endregion

        #region 公开属性

        /// <summary>
        /// 文档的字符数量。段落之间，使用 `\r\n` 换行符，加入计算为两个字符。包含项目符号
        /// </summary>
        public int CharCount
        {
            get
            {
                var sum = 0;
                foreach (var paragraphData in DocumentRunEditProvider.ParagraphManager.GetParagraphList())
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
        /// 设置当前文本的默认字符属性
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        public void SetDefaultTextRunProperty<T>(Action<T> config) where T : IReadOnlyRunProperty
        {
            CurrentRunProperty =
                TextEditor.PlatformRunPropertyCreator.BuildNewProperty(property => config((T)property),
                    CurrentRunProperty);
        }

        /// <summary>
        /// 设置当前光标的字符属性。在光标切走之后，自动失效
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        /// <param name="config"></param>
        public void SetCurrentCaretRunProperty<T>(Action<T> config) where T : IReadOnlyRunProperty
        {
            // 先获取当前光标的字符属性吧
            IReadOnlyRunProperty currentCaretRunProperty = CurrentCaretRunProperty;

            CaretManager.CurrentCaretRunProperty = TextEditor.PlatformRunPropertyCreator.BuildNewProperty(
                property => config((T)property),
                currentCaretRunProperty);
        }

        /// <summary>
        /// 将设置一段范围内的文本的字符属性。如传入的 <paramref name="selection"/> 为空，且当前也没有选择内容，则仅修改当前光标的字符属性
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        /// <param name="config"></param>
        /// <param name="selection">如为空，则采用当前选择内容。如当前也没有选择内容，则仅修改当前光标的字符属性</param>
        public void SetRunProperty<T>(Action<T> config, Selection? selection) where T : IReadOnlyRunProperty
        {
            // 如为空，则采用当前选择内容
            selection ??= CaretManager.CurrentSelection;

            // 如当前也没有选择内容，则仅修改当前光标的字符属性
            if (selection.Value.IsEmpty)
            {
                SetCurrentCaretRunProperty(config);
            }
            else
            {
                throw new NotImplementedException("设置范围内的文本的字符属性");
            }
        }

        #endregion

        /// <summary>
        /// 追加一段文本
        /// </summary>
        /// 其实这个方法不应该放在这里
        internal void AppendText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                // 空白内容
                return;
            }

            TextEditor.AddLayoutReason(nameof(AppendText));

            //// 追加在字符数量，也就是最末
            //EditAndReplaceRun(this.GetDocumentEndSelection(), new TextRun(text));

            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);

            // 设置光标到文档最后，再进行追加。设置光标到文档最后之后，可以自动获取当前光标下的文本字符属性
            CaretManager.CurrentCaretOffset = new CaretOffset(CharCount);

            var textRun = new TextRun(text, CurrentCaretRunProperty);
            DocumentRunEditProvider.Append(textRun);

            CaretManager.CurrentCaretOffset = new CaretOffset(CharCount);

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="run"></param>
        public void EditAndReplaceRun(Selection selection, IImmutableRun run)
        {
            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);
            // 这里只处理数据变更，后续渲染需要通过 InternalDocumentChanged 事件触发

            DocumentRunEditProvider.Replace(selection, run);

            var caretOffset = new CaretOffset(selection.BehindOffset.Offset - selection.Length + run.Count);
            CaretManager.CurrentCaretOffset = caretOffset;

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region UndoRedo
        // 这里的方法只允许撤销恢复调用

        internal void SetDefaultTextRunPropertyByUndoRedo(IReadOnlyRunProperty runProperty)
        {
            TextEditor.VerifyInUndoRedoMode();
            CurrentRunProperty = runProperty;
        }

        #endregion
    }
}