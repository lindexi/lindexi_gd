using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace RuhuyagayBemkaijearfear
{
    static class BrushCreator
    {
        /// <summary>
        /// 尝试从缓存获取或创建颜色笔刷
        /// </summary>
        /// <param name="color">对应的字符串颜色</param>
        /// <returns>已经被 Freeze 的颜色笔刷</returns>
        public static SolidColorBrush GetOrCreate(string color)
        {
            if (!color.StartsWith("#"))
            {
                throw new ArgumentException($"输入的{nameof(color)}不是有效颜色，需要使用 # 开始");
                // 如果不使用 # 开始将会在 ConvertFromString 出现异常
            }

            if (TryGetBrush(color, out var brushValue))
            {
                return (SolidColorBrush)brushValue;
            }

            object convertColor;

            try
            {
                convertColor = ColorConverter.ConvertFromString(color);
            }
            catch (FormatException)
            {
                // 因为在 ConvertFromString 会抛出的是 令牌无效 难以知道是为什么传入的不对
                throw new ArgumentException($"输入的{nameof(color)}不是有效颜色");
            }

            if (convertColor == null)
            {
                throw new ArgumentException($"输入的{nameof(color)}不是有效颜色");
            }

            var brush = new SolidColorBrush((Color)convertColor);
            if (TryFreeze(brush))
            {
                BrushCacheList.Add(color, new WeakReference<Brush>(brush));
            }

            return brush;
        }

        private static Dictionary<string, WeakReference<Brush>> BrushCacheList { get; } =
            new Dictionary<string, WeakReference<Brush>>();

        private static bool TryGetBrush(string key, out Brush brush)
        {
            if (BrushCacheList.TryGetValue(key, out var brushValue))
            {
                if (brushValue.TryGetTarget(out brush))
                {
                    return true;
                }
                else
                {
                    // 被回收的资源
                    BrushCacheList.Remove(key);
                }
            }

            brush = null;
            return false;
        }

        private static bool TryFreeze(Freezable freezable)
        {
            if (freezable.CanFreeze)
            {
                freezable.Freeze();
                return true;
            }

            return false;
        }
    }
}