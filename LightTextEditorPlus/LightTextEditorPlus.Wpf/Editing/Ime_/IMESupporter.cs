using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing
{
    /// <summary>
    /// 为控件提供输入法的支持
    /// </summary>
    /// 代码是从 WPF 代码仓库抄的，请看 wpf\src\Microsoft.DotNet.Wpf\src\PresentationFramework\System\Windows\Documents\ImmComposition.cs 文件
    /// 代码也从 https://github.com/icsharpcode/AvalonEdit/blob/master/ICSharpCode.AvalonEdit/Editing/ImeSupport.cs 抄了部分
    /// 另外的配置也从 https://github.com/cefsharp/CefSharp/blob/bfa8ccf24c7694a80ec42b8f3d6d1683b144ec68/CefSharp.Wpf/Internals/IMEHandler.cs 抄了部分
    /// 相关文档：
    /// [Input Context - Win32 apps | Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/intl/input-context )
    /// [ImmGetContext function (imm.h) - Win32 apps | Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/api/imm/nf-imm-immgetcontext )
    /// [WM_IME_COMPOSITION message (Winuser.h) - Win32 apps | Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/intl/wm-ime-composition )
    /// [WM_IME_COMPOSITION message (Winuser.h) - Win32 apps | Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/intl/wm-ime-composition )
    /// [Input Method Manager - Win32 apps | Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/intl/input-method-manager )
    /// [我的Win32输入法编程心得](https://baohaojun.github.io/blog/2013/10/04/0-Win32-IME-Programming.html )
    /// IMM: Input Method Manager, 输入法管理器 https://docs.microsoft.com/en-us/windows/desktop/Intl/input-method-manager
    /// IME: Input Method Editor Engine, 输入法编辑器, 引擎 
    internal class IMESupporter<T> where T : UIElement, IIMETextEditor
    {
        // ReSharper disable InconsistentNaming
        public IMESupporter(T editor)
        {
            Editor = editor;
            Editor.GotKeyboardFocus += Editor_GotKeyboardFocus;
            Editor.LostKeyboardFocus += Editor_LostKeyboardFocus;

            InputMethod.SetIsInputMethodSuspended(editor, true);
        }

        private void Editor_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Log($"GotKeyboardFocus");

            if (Editor.IsKeyboardFocused)
            {
                if (_hwndSource != null)
                    return;
                _isUpdatingCompositionWindow = true;
                CreateContext();
            }
            else
            {
                ClearContext();
                return;
            }

            UpdateCompositionWindow();
            _isUpdatingCompositionWindow = false;
        }

        private void Editor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Log($"LostKeyboardFocus");
            if (_isUpdatingCompositionWindow)
                return;
            if (Equals(e.OldFocus, Editor) && _currentContext != IntPtr.Zero)
            {
                IMENative.ImmNotifyIME(_currentContext, IMENative.NI_COMPOSITIONSTR, IMENative.CPS_CANCEL);
            }

            ClearContext();
        }

        private void CreateContext()
        {
            ClearContext();
            _hwndSource = (HwndSource) (PresentationSource.FromVisual(Editor) ??
                                       throw new ArgumentNullException(nameof(Editor)));

            _defaultImeWnd = IMENative.ImmGetDefaultIMEWnd(IntPtr.Zero);
            Log($"_defaultImeWnd={_defaultImeWnd}");
            if (_defaultImeWnd == IntPtr.Zero)
            {
                // 如果拿到了空的默认 IME 窗口了，那么此时也许是作为嵌套窗口放入到另一个进程的窗口
                // 拿不到就需要刷新一下。否则微软拼音输入法将在屏幕的左上角上
                RefreshInputMethodEditors();

                // 尝试通过 _hwndSource 也就是文本所在的窗口去获取
                _defaultImeWnd = IMENative.ImmGetDefaultIMEWnd(_hwndSource.Handle);
                Log($"_defaultImeWnd2={_defaultImeWnd}");

                if (_defaultImeWnd == IntPtr.Zero)
                {
                    // 如果依然获取不到，那么使用当前激活的窗口，在准备输入的时候
                    // 当前的窗口大部分都是对的
                    // 进入这里，是尽可能恢复输入法，拿到的 GetForegroundWindow 虽然预计是不对的
                    // 也好过没有输入法
                    _defaultImeWnd = IMENative.ImmGetDefaultIMEWnd(Win32.User32.GetForegroundWindow());
                    Log($"_defaultImeWnd3={_defaultImeWnd}");
                }
            }

            // 使用 DefaultIMEWnd 可以比较好解决微软拼音的输入法到屏幕左上角的问题
            _currentContext = IMENative.ImmGetContext(_defaultImeWnd);
            Log($"_currentContext={_currentContext}");
            if (_currentContext == IntPtr.Zero)
            {
                _currentContext = IMENative.ImmGetContext(_hwndSource.Handle);
                Log($"_currentContext2={_currentContext}");
            }

            // 对 Win32 使用第一套输入法框架的输入法，可以采用 ImmAssociateContext 关联
            // 但是对实现 TSF 第二套输入法框架的输入法，在应用程序对接第二套输入法框架
            // 就需要调用 ITfThreadMgr 的 SetFocus 方法。刚好 WPF 对接了
            _previousContext = IMENative.ImmAssociateContext(_hwndSource.Handle, _currentContext);
            _hwndSource.AddHook(WndProc);

            // 尽管文档说传递null是无效的，但这似乎有助于在与WPF共享的默认输入上下文中激活IME输入法
            // 这里需要了解的是，在 WPF 的逻辑，是需要传入 DefaultTextStore.Current.DocumentManager 才符合预期
            IMENative.ITfThreadMgr? threadMgr = IMENative.GetTextFrameworkThreadManager();
            threadMgr?.SetFocus(IntPtr.Zero);
        }

        private void ClearContext()
        {
            if (_hwndSource == null)
                return;
            IMENative.ImmAssociateContext(_hwndSource.Handle, _previousContext);
            IMENative.ImmReleaseContext(_defaultImeWnd, _currentContext);
            _currentContext = IntPtr.Zero;
            _defaultImeWnd = IntPtr.Zero;
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        /// <summary>
        /// 更新CompositionWindow位置，用于跟随输入光标。调用此方法时，将从 <see cref="IIMETextEditor"/> 获取参数
        /// </summary>
        public void UpdateCompositionWindow()
        {
            if (_currentContext == IntPtr.Zero)
            {
                CreateContext();
                if (_currentContext == IntPtr.Zero)
                {
                    return;
                }
            }

            // 这是判断在系统版本大于 Win7 的系统，如 Win10 系统上，使用微软拼音输入法的逻辑
            // 微软拼音输入法在几个版本，需要修改 Y 坐标，加上输入的行高才可以。但是在一些 Win10 版本，通过补丁又修了这个问题
            //_isSoftwarePinYinOverWin7 = IsMsInputMethodOverWin7();
            //上面判断微软拼音的方法，会导致方法被切片，从而在快速得到焦点和失去焦点时，失去焦点清理的代码会先于此函数执行，导致引发错误
            if (_hwndSource == null)
                return;
            SetCompositionFont();
            SetCompositionWindow();
        }

        private void SetCompositionFont()
        {
            var lf = new IMENative.LOGFONT();
            lf.lfFaceName = Editor.GetFontFamilyName();
            lf.lfHeight = Editor.GetFontSize();

            var hIMC = _currentContext;

            var GCS_COMPSTR = 8;

            var length = IMENative.ImmGetCompositionString(hIMC, GCS_COMPSTR, null, 0);
            if (length > 0)
            {
                var target = new byte[length];
                var count = IMENative.ImmGetCompositionString(hIMC, GCS_COMPSTR, target, length);
                if (count > 0)
                {
                    var inputString = Encoding.Default.GetString(target);
                    if (string.IsNullOrWhiteSpace(inputString))
                    {
                        lf.lfWidth = 1;
                    }
                }
            }

            Log($"ImmSetCompositionFont");
            IMENative.ImmSetCompositionFont(hIMC, ref lf);
        }

        private void SetCompositionWindow()
        {
            var hIMC = _currentContext;
            HwndSource source = _hwndSource ?? throw new ArgumentNullException(nameof(_hwndSource));

            var textEditorLeftTop = Editor.GetTextEditorLeftTop();
            var caretLeftTop = Editor.GetCaretLeftTop();

            var transformToAncestor = Editor.TransformToAncestor(source.RootVisual);

            var textEditorLeftTopForRootVisual = transformToAncestor.Transform(textEditorLeftTop);
            var caretLeftTopForRootVisual = transformToAncestor.Transform(caretLeftTop);

            //解决surface上输入法光标位置不正确
            //现象是surface上光标的位置需要乘以2才能正确，普通电脑上没有这个问题
            //且此问题与DPI无关，目前用CaretWidth可以有效判断
            caretLeftTopForRootVisual = new Point(caretLeftTopForRootVisual.X / SystemParameters.CaretWidth,
                caretLeftTopForRootVisual.Y / SystemParameters.CaretWidth);

            //const int CFS_DEFAULT = 0x0000;
            //const int CFS_RECT = 0x0001;
            const int CFS_POINT = 0x0002;
            //const int CFS_FORCE_POSITION = 0x0020;
            //const int CFS_EXCLUDE = 0x0080;
            //const int CFS_CANDIDATEPOS = 0x0040;

            var form = new IMENative.CompositionForm();
            form.dwStyle = CFS_POINT;
            form.ptCurrentPos.x = (int) Math.Max(caretLeftTopForRootVisual.X, textEditorLeftTopForRootVisual.X);
            form.ptCurrentPos.y = (int) Math.Max(caretLeftTopForRootVisual.Y, textEditorLeftTopForRootVisual.Y);
            //if (_isSoftwarePinYinOverWin7)
            //{
            //    form.ptCurrentPos.y += (int) characterBounds.Height;
            //}

            Log($"ImmSetCompositionWindow x={form.ptCurrentPos.x} y={form.ptCurrentPos.y}");
            IMENative.ImmSetCompositionWindow(hIMC, ref form);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (msg)
            {
                case IMENative.WM_INPUTLANGCHANGE:
                    Log($"WM_INPUTLANGCHANGE");
                    if (_hwndSource != null)
                    {
                        CreateContext();
                    }

                    break;
                case IMENative.WM_IME_COMPOSITION:
                    Log($"WM_IME_COMPOSITION");
                    UpdateCompositionWindow();
                    break;

                    //case (int) Win32.WM.IME_NOTIFY:
                    // 根据 WPF 的源代码，是需要在此消息里，调用 ImmSetCandidateWindow 进行更新的
                    // 但是似乎不写也没啥锅，于是就先不写了
                    // 下次遇到，可以了解到这里还没有完全抄代码
                    //    {
                    //        Debug.WriteLine("IME_NOTIFY");
                    //        break;
                    //    }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 刷新 IME 的 ITfThreadMgr 状态，用于修复打开 Win32Dialog 之后关闭，输入法无法输入中文问题
        /// </summary>
        /// 原因是在打开 Win32Dialog 之后，将会让 ITfThreadMgr 失去焦点。因此需要使用本方法刷新，通过 InputMethod 的 IsInputMethodEnabledProperty 属性调用到 InputMethod 的 EnableOrDisableInputMethod 方法，在这里面调用到 TextServicesContext.DispatcherCurrent.SetFocusOnDefaultTextStore 方法，从而调用到 SetFocusOnDim(DefaultTextStore.Current.DocumentManager) 的代码，将 DefaultTextStore.Current.DocumentManager 设置为 ITfThreadMgr 的焦点，重新绑定 IME 输入法
        /// 但是即使如此，依然拿不到 <see cref="_defaultImeWnd"/> 的初始值。依然需要重新打开和关闭 WPF 窗口才能拿到
        /// [Can we public the `DefaultTextStore.Current.DocumentManager` property to create custom TextEditor with IME · Issue #6139 · dotnet/wpf](https://github.com/dotnet/wpf/issues/6139 )
        private void RefreshInputMethodEditors()
        {
            if (InputMethod.GetIsInputMethodEnabled(Editor))
            {
                InputMethod.SetIsInputMethodEnabled(Editor, false);
            }

            if (InputMethod.GetIsInputMethodSuspended(Editor))
            {
                InputMethod.SetIsInputMethodSuspended(Editor, false);
            }

            InputMethod.SetIsInputMethodEnabled(Editor, true);
            InputMethod.SetIsInputMethodSuspended(Editor, true);
        }

        private T Editor { get; }

        private IntPtr _defaultImeWnd;
        private IntPtr _currentContext;
        private IntPtr _previousContext;
        private HwndSource? _hwndSource;

        private bool _isUpdatingCompositionWindow;

        private static void Log(string message)
        {
            Debug.WriteLine($"[IMESupport] {message}");
        }
    }
}