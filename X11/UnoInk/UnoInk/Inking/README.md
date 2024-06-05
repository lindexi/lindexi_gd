# 代码框架

![](./Docs/Image/Image1.png)


1. 从 UNO 接收输入，在 X11 绘制
   - 开发 01781d31ae7c06e094dabfb6f80d81a0877bd8ef t/lindexi/Ink 可以切分支
2. 从 UNO 接收输入，在 UNO 绘制，在 X11 显示
   - 可基于 42cfb47f083c60c0ec37f30be10d2ed945ae1123 重新开发
3. 从 X11 接收输入，在 X11 绘制，判断 UNO 命中
   - 去掉 ddad66981bd175e1ef84bd3bd547dc329964981c 即可使用，效果很好

- [x] 支持 SkiaVisual 功能
- [x] 跨线程使用 skPath 将会炸掉应用
  - 最简复现 c82dcaf20da0948aede539b699f47926635b94a3
  - 实际原因是 SkInkCanvas 的 InkStrokePath 在 DrawStrokeContext 的 Dispose 被释放。加上 01fd5aebad41efef3ec9afaaaefcd30a0d674cb0 即可解决
- [x] 是否需要调用 XInitThreads 方法
  - 不需要
- [x] 获取本地窗口方法
