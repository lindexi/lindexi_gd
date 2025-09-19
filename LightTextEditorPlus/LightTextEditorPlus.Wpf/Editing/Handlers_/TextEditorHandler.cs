using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Editing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{
    public virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Insert)
        {
            SwitchOvertypeMode();
            e.Handled = true;
            return;
        }
        else if (e.Key == Key.Enter)
        {
            BreakLine();
            e.Handled = true;
            return;
        }
    }

    public virtual void OnTextInput(TextCompositionEventArgs e)
    {
        if (e.Handled ||
            string.IsNullOrEmpty(e.Text) ||
            e.Text == "\x1b" ||
            // 退格键 \b 键
            e.Text == "\b" ||
            //emoji包围符
            e.Text == "\ufe0f")
            return;

        RawTextInput(e.Text);
    }

    #region 方向键

    public virtual partial void MoveCaret(CaretMoveType type)
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
        var visual = TextEditor;
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
}
