namespace SkiaInkCore.Settings;

/// <summary>
/// 橡皮擦算法模式
/// </summary>
enum InkCanvasEraserAlgorithmMode
{
    /// <summary>
    /// 是否允许使用裁剪方式的橡皮擦，而不是走静态笔迹层。使用裁剪而不是使用笔迹计算，将笔迹的点给去掉
    /// </summary>
    EnableClippingEraser,

    /// <summary>
    /// 是否允许使用裁剪方式的橡皮擦，橡皮擦每次裁剪都写入画布，需要有多余的画布拷贝逻辑，但是不需要做 Path 处理。原理同 <see cref="EnableClippingEraser"/> 但是具体的裁剪逻辑不相同。用来减少擦除时间长时的越擦越卡的问题
    /// </summary>
    EnableClippingEraserWithoutEraserPathCombine,

    /// <summary>
    /// 是否允许使用裁剪方式的橡皮擦，带不安全模式的二进制，橡皮擦每次裁剪都写入画布，需要有多余的画布拷贝逻辑，但是不需要做 Path 处理。原理同 <see cref="EnableClippingEraserWithoutEraserPathCombine"/> 但是具体的裁剪逻辑不相同。用来减少擦除时间长时的越擦越卡的问题。带不安全的二进制处理可以提升画图片的性能
    /// </summary>
    EnableClippingEraserWithBinaryWithoutEraserPathCombine,

    /// <summary>
    /// 进行点和 Path 的命中测试的橡皮擦，真实擦掉笔迹点和 Path 的橡皮擦
    /// </summary>
    EnablePointPathEraser,
}