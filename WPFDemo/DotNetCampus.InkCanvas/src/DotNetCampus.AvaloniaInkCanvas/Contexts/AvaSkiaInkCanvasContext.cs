using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Inking.Contexts;

internal class AvaSkiaInkCanvasContext
{
    private bool _isUsingBitmapCache;

    /// <summary>
    /// 获取可以由业务指定的笔迹设置。
    /// </summary>
    public AvaSkiaInkCanvasSettings Settings { get; } = new();

    /// <summary>
    /// 如果指定为 <see langword="true"/>，则立即使用位图缓存替代真实的笔迹。<br/>
    /// 否则，位图缓存将不会工作，而使用真实的笔迹渲染。
    /// </summary>
    /// <remarks>
    /// 在合适的时机切换真实的笔迹和位图缓存，可能可以提高性能。<br/>
    /// 例如，在书写时，使用真实的笔迹可以提高书写性能；在漫游时，使用位图缓存可以提高漫游性能。
    /// </remarks>
    public bool ShouldUseBitmapCache => Settings.IsBitmapCacheEnabled && _isUsingBitmapCache;

    /// <summary>
    /// 指定是否立即使用位图缓存替代真实的笔迹，或是使用真实的笔迹渲染。
    /// </summary>
    /// <param name="useBitmapCache">
    /// 指定为 <see langword="true"/>，则立即使用位图缓存替代真实的笔迹；<br/>
    /// 指定为 <see langword="false"/>，则位图缓存将不会工作，而使用真实的笔迹渲染。
    /// </param>
    /// <remarks>
    /// 注意，使用此方法和修改用户设置方法 <see cref="AvaSkiaInkCanvasSettings.IsBitmapCacheEnabled"/> 的本质不同为：
    /// <list type="number">
    /// <item>此方法决定在不同的程序状态下是否应使用位图缓存，例如书写时不应开启缓存，而漫游时应该开启缓存；</item>
    /// <item>而修改用户设置则是一个开关选项，仅决定在上述合适的状态下是否应使用位图缓存。当遇到不合适的程序状态时位图缓存依然不会生效。</item>
    /// </list>
    /// 所以，正确的做法是：
    /// <list type="bullet">
    /// <item>应用开发者在应用程序初始化时设置用户设置 <see cref="AvaSkiaInkCanvasSettings.IsBitmapCacheEnabled"/>；</item>
    /// <item>框架开发者在实现位图缓存时，在适当的时机开启和关闭位图缓存，例如书写时关闭，漫游时开启。</item>
    /// </list>
    /// </remarks>
    public void UseBitmapCache(bool useBitmapCache)
    {
        _isUsingBitmapCache = useBitmapCache;
    }
}