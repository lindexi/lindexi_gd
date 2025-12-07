//using System;
//using LightTextEditorPlus.Core.Document;
//using LightTextEditorPlus.Document.Decorations;
//#if USE_SKIA
//using SkiaSharp;
//#endif

//namespace LightTextEditorPlus.Document;

//#pragma warning disable CS1574, CS1584, CS1581, CS1580 // 忽略一些找不到的类型

///// <summary>
///// 文本字符属性
///// </summary>
///// 设计上文本字符属性是只读的，但是 IReadOnlyRunProperty 命名已用
///// 为什么需要再定义一个接口？因为文本的字符属性是用到了很多平台相关属性，有些定义是有些平台不支持的
///// 这个接口是用来框架外的业务层的，在框架内是不使用接口的
///// 此接口用于抹平平台差异，提供一个统一的接口给业务层使用。核心原因是考虑到 Avalonia 或 MAUI 或 UNO 平台是采用 Skia 间接提供能力的，如果也期望有一个和框架适配的文本字符属性类型，自然这个类型就应该命名为 RunProperty 了。但是 Skia 里面也有一个 RunProperty 的概念，于是只好叫 <see cref="SkiaTextRunProperty"/> 了
///// 但是，但是，在 Avalonia 的 AllInOne 里面又要将 <see cref="SkiaTextRunProperty"/> 捡起来，但却不是作为第一实现代码。这就很复杂了
///// 于是这样，相同的字符属性概念就有多个类型名，要是在各个使用到字符属性的地方，都使用 using 的方式写，自然是不方便的。为此就引入了 <see cref="IRunProperty"/> 接口，期望通过接口让 Avalonia 等框架不污染类型名
///// 对于共享的代码来说，能用 <see cref="IRunProperty"/> 接口，就尽量用 <see cref="IRunProperty"/> 接口，保持代码不需要上 #if 的魔法
//public interface IRunProperty : IReadOnlyRunProperty, IEquatable<IRunProperty>
//{
//    /// <summary>
//    /// 不透明度
//    /// </summary>
//    double Opacity { get; }

//    /// <summary>
//    /// 文本的装饰集合
//    /// </summary>
//    TextEditorImmutableDecorationCollection DecorationCollection { get; }

//#if USE_WPF || USE_AVALONIA

//#if USE_WPF
//    /// <summary>
//    /// 前景色
//    /// </summary>
//    ImmutableBrush Foreground { get; }

//    /// <summary>
//    /// 背景色
//    /// </summary>
//    ImmutableBrush? Background { get; } 

//    /// <summary>
//    /// 获取描述与某个字体与该字体的正常纵横比相比的拉伸程度
//    /// </summary>
//    FontStretch Stretch { get; }

//    /// <summary>
//    /// 获取字重
//    /// </summary>
//    FontWeight FontWeight { get; }

//    /// <summary>
//    /// 获取字体样式
//    /// </summary>
//    FontStyle FontStyle { get; }
//#endif

//#elif USE_SKIA
//    SKColor Foreground { get; }
//    SKColor Background { get; }

//    SKFontStyleWidth Stretch { get; }

//    SKFontStyleWeight FontWeight { get; }
//    SKFontStyleSlant FontStyle { get; }
//#endif

//}

