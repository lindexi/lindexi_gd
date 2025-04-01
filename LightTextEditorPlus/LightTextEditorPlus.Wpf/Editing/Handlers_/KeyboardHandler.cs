using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Editing;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 用来处理键盘的交互行为
/// </summary>
/// 不包含 IME 输入法的行为。关于 IME 输入法相关，放在 IME 相关模块里处理
internal class KeyboardHandler
{
    public KeyboardHandler(TextEditor textEditor)
    {
        _textArea = textEditor;

        //光标移动
        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveLeftByCharacter,
            MoveCaret(CaretMoveType.LeftByCharacter)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveLeftByCharacter, Key.Left, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveRightByCharacter,
            MoveCaret(CaretMoveType.RightByCharacter)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveRightByCharacter, Key.Right, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveUpByLine,
            MoveCaret(CaretMoveType.UpByLine)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveUpByLine, Key.Up, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveDownByLine,
            MoveCaret(CaretMoveType.DownByLine)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveDownByLine, Key.Down, ModifierKeys.None));

        //编辑
        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, OnBackspace));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.Backspace, Key.Back, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.Delete, OnDelete));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.Delete, Key.Delete, ModifierKeys.None));

        textEditor.KeyDown += TextEditor_KeyDown;
    }

    private void TextEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Insert)
        {
            if (TextEditor.CheckFeaturesDisableWithLog(TextFeatures.OvertypeModeEnable))
            {
                return;
            }

            TextEditor.IsOvertypeMode = !TextEditor.IsOvertypeMode;
            e.Handled = true;
        }
    }

    #region 删除

    private static void OnDelete(object sender, ExecutedRoutedEventArgs e)
    {
        var textEditor = (TextEditor) e.Source;
        textEditor.TextEditorCore.Delete();
    }

    private static void OnBackspace(object sender, ExecutedRoutedEventArgs e)
    {
        var textEditor = (TextEditor) e.Source;
        textEditor.TextEditorCore.Backspace();
    }

    #endregion

    #region 方向键

    private ExecutedRoutedEventHandler MoveCaret(CaretMoveType moveType)
    {
        return (o, args) =>
        {
            //var textEditor = (TextEditor)args.Source; // 就是从 TextEditor 订阅的
            MoveCaretInner(moveType);
        };
    }

    private void MoveCaretInner(CaretMoveType type)
    {
        type = TransformCaretMove(type);
        TextEditor.TextEditorCore.MoveCaret(type);
    }

    /// <summary>
    /// 根据文本框实际的视觉上的旋转角度，优化键盘方向。让键盘方向控制的光标符合正视觉方向
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private CaretMoveType TransformCaretMove(CaretMoveType type)
    {
        CaretMoveType returnValue = (CaretMoveType) TransformDirection((int) type);

        return returnValue;
    }

    /// <summary>
    /// 根据文本框实际的视觉上的旋转角度，优化键盘方向的统一方法
    /// 因为存在很多个和方向相关的类型，例如 <see cref="CaretMoveType"/> 和 <see cref="SelectionType"/> 等，为了让这些逻辑的方向键统一计算，因此加了此方法
    /// </summary>
    /// <param name="direction">输入值采用 int 是为了兼容多个类型的枚举</param>
    /// <returns></returns>
    private int TransformDirection(int direction)
    {
        // 算法是：
        // 先获取枚举值里面的方向部分，将方向部分进行旋转，将更新之后的方向更新回原先的枚举值

        // 1. 获取仅方向
        // 获取的方法是通过二进制计算，通过 与 逻辑，只取方向的部分的数据
        // 例如 direction = 0B1000_0010
        // DirectionMask  = 0B0000_1111
        // 这两个值执行 与 逻辑，将是 0B1000_0010 & 0B0000_1111 = 0B0000_0010
        // 于是获取方向部分的值
        var directionValue = (Direction) (direction & (int) DirectionMask.DirectionMask);

        // 2. 获取旋转之后的方向
        var newDirection = TransformDirection(directionValue);

        // 3. 将新的旋转之后的方向更新上去
        // 3.1 获取去掉方向的值
        // 更新的方法就是先去掉原有的方向值，去掉的方法就是先对 DirectionMask 取反，也就是除了方向部分的值是全零之外，其他都是全一的值
        // 执行 与 逻辑，即可获取方向部分的值是全零，而其他部分保持不变的新值
        var maskType = (direction & (~(int) DirectionMask.DirectionMask));

        // 3.2 拼接原有的其他属性
        // 拼接的做法就是执行 或 逻辑，将原本方向部分的值是全零与新的方向取 或 逻辑，即可让方向部分的值等于新的方向的值，其他部分不变
        var returnValue = maskType | (int) newDirection;

        return returnValue;
    }
    /// <summary>
    /// 根据实际的视觉旋转角度，修改方向。让键盘方向控制的光标符合正视觉方向
    /// </summary>
    /// <remarks>
    /// 文档: KB290830056
    /// </remarks>
    /// <param name="direction"></param>
    /// <returns></returns>
    private Direction TransformDirection(Direction direction)
    {
        if (direction == Direction.None)
        {
            // 如果不包括方向，那么不执行处理逻辑
            return direction;
        }

        var angle = GetTextAreaRotationAngle();

        Direction returnValue;

        if ((angle <= 0 && angle >= -75) || (angle > 0 && angle <= 75))
        {
            // -75-75 度之间，左右依然是左右，上下依然是上下
            returnValue = direction;
        }
        else if (angle > 75 && angle <= 105)
        {
            // 75-105度，左=下，右=上，上=左，下=右
            returnValue = direction switch
            {
                Direction.Left => Direction.Down,
                Direction.Right => Direction.Up,
                Direction.Up => Direction.Left,
                Direction.Down => Direction.Right,
                _ => direction,
            };
        }
        else if ((angle > 105 && angle <= 180) || (angle <= -105 && angle >= -180))
        {
            // 105-255度，左=右，右=左，上=下，下=上
            // 105-255度 = 105-180 度 + （ -105）-（-180）度
            returnValue = direction switch
            {
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                _ => direction,
            };
        }
        else if (angle < -75 && angle > -105)
        {
            // 255-285度，左=上，右=下，上=右，下=左
            // 255-285度 =（-75）- （-105）度
            returnValue = direction switch
            {
                Direction.Left => Direction.Up,
                Direction.Right => Direction.Down,
                Direction.Up => Direction.Right,
                Direction.Down => Direction.Left,
                _ => direction,
            };
        }
        else
        {
            // 理论上不可能进入此分支，没有在 -180 < angle < 180 的值
            throw new ArgumentOutOfRangeException(nameof(angle));
        }

        //TextTracer.Debug($"角度：{angle}；变更键盘方向键从 {direction} 到 {returnValue}", nameof(KeyboardHandler));

        return returnValue;
    }

    /// <summary>
    /// 获取显示效果的文本框旋转角度
    /// </summary>
    /// <returns>返回 0-180 度和 0-(-180) 度范围的角度</returns>
    private double GetTextAreaRotationAngle()
    {
        // 内部变换
        var visual = _textArea;
        var root = GetRootVisual(visual);
        var transform = ((MatrixTransform) visual.TransformToAncestor(root)).Value;

        // 计算旋转分量
        var unitVector = new Vector(1, 0);
        var vector = transform.Transform(unitVector);
        var angle = Vector.AngleBetween(unitVector, vector);
        return angle;
    }

    private static Visual GetRootVisual(Visual visual)
    {
        if (visual == null) throw new ArgumentNullException(nameof(visual));

        var root = visual;
        var parent = VisualTreeHelper.GetParent(visual);
        while (parent != null)
        {
            if (parent is Visual r)
            {
                root = r;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return root;
    }
    #endregion

    private TextEditor TextEditor => _textArea;

    /// <summary>
    /// 文本交互范围
    /// </summary>
    /// 只是为了方便抄代码，后续可以干掉
    private readonly TextEditor _textArea;
}