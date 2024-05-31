# 代码框架

![](./Docs/Image/Image1.png)


1. 从 UNO 接收输入，在 X11 绘制
   - 更新笔迹算法，降低延迟
   - 多线程同步和调度
   - 窗口层级关系
   - 停止渲染问题
   - 窗口穿透
   - 开发 01781d31ae7c06e094dabfb6f80d81a0877bd8ef t/lindexi/Ink 可以切分支
2. 从 UNO 接收输入，在 UNO 绘制，在 X11 显示
   - 可基于 42cfb47f083c60c0ec37f30be10d2ed945ae1123 重新开发
3. 从 X11 接收输入，在 X11 绘制，判断 UNO 命中
   - 去掉 ddad66981bd175e1ef84bd3bd547dc329964981c 即可使用，效果很好
