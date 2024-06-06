# 代码框架

![](./Docs/Image/Image1.png)


1. 从 UNO 接收输入，在 X11 绘制
   - 开发 01781d31ae7c06e094dabfb6f80d81a0877bd8ef t/lindexi/Ink 可以切分支
2. 从 UNO 接收输入，在 UNO 绘制，在 X11 显示
   - 可基于 42cfb47f083c60c0ec37f30be10d2ed945ae1123 重新开发
3. 从 X11 接收输入，在 X11 绘制，判断 UNO 命中
   - 去掉 ddad66981bd175e1ef84bd3bd547dc329964981c 即可使用，效果很好

- [ ] 落笔时闪烁
  - 尝试解决 df33f57b3c74a331e6651cb44dfdb4ee351f496c 无效
  - 尝试修复按下的点被记录，在移动被错误读取 9e4fecc95ecc7cc2b2fe62f697332d1883e0a1c3 无效
  - [x] 尝试将上层发送的点和底层接收到的点记录起来 看起来点似乎正常
  - [x] 测试 SKPathFillType 的行为 完成测试，不会出现空的情况
  - [x] 尝试修改支持多个点的输入
    - 笔迹点多了会卡
    - 依然落笔卡顿和闪烁
- [ ] 启动时会闪烁窗口
- [ ] 动态笔迹重新命名为 DynamicRenderer 类型，保持和 WPF 相同
- [ ] 优化抬笔闪烁
  - 核心原因是渲染同步
  - 尝试使用还没合入的方法
- [x] 下一笔落下才能更新上一笔
  - 似乎是 UNO 的坑，经过了 8128f2e57cd68cfd283dd0a809fd6af34f956922 的输出测试，可以看到确实有绘制了笔迹内容
  - 使用 352158d626769f20240523d3350fa0db3f4caeba 可以修复，证明是 Uno 的问题，在 OnPaintSurface 不会自动刷新界面
- [x] 界面停止渲染或 X11的 XShapeCombineRegion 方法不会返回
  - 经过测试 点击命中穿透 的 SetClickThrough 不返回 7df81f1db2cc9ffa6c4b1dbdcfcc97308e551e96 回滚代码却能正常，似乎受到了 258a60849bcee8adab16c45b2303bb5f8e096058 的影响，但这里改动的是序列化。同步逐步抄代码，找到是在 369f36d6523bf1789b43b82d3c39e43d0b68ba96 这里没有执行渲染。还原代码 e7a4336a067f599aa6ece29c2d17b393427d2a97 依然正常，不知道炸哪里。加上输出信息之后，可以正常执行命中窗口方法，也进行返回 e24afd4f1281e1ebba950bd4e73adaaec4eb70df 不知道有什么关系
  - 只要主线程有输出，随便输出，就能修复 XShapeCombineRegion 方法不会返回。最简修复代码 fa08b6854bd9d43445fa3d9e93cb2ebc1d4a9cca
- [x] 测试给 UNO 注入多点触摸 86984cb5eab3fd16df49ab173ec129b7bdf7ec0e
- [x] 测试不开启另一个窗口是否命中
  - b2881a8307d73f42906da9ab18dfc03cc93ebd0c
  - 可以命中
- [x] 跨线程使用 skPath 将会炸掉应用
  - 最简复现 c82dcaf20da0948aede539b699f47926635b94a3
  - 实际原因是 SkInkCanvas 的 InkStrokePath 在 DrawStrokeContext 的 Dispose 被释放。加上 01fd5aebad41efef3ec9afaaaefcd30a0d674cb0 即可解决
- [x] 是否需要调用 XInitThreads 方法
  - 不需要
- [x] 获取本地窗口方法


```
完成 UNO 绘制
DeviceNumber=2
XIDeviceInfo [0] 2 XIMasterPointer
ABS_MT_TOUCH_MAJOR=281 Name=Abs MT Touch Major ABS_MT_TOUCH_MINOR=282 Name=Abs MT Touch Minor Abs_MT_Pressure=286 Name=Abs MT Pressure
pointerDevice.Value.NumClasses=8
完成初始化
没有按下就移动！！！InkCanvas_OnPointerMoved
InkCanvas_OnPointerMoved
没有按下就移动！！！InkCanvas_OnPointerMoved
执行移动 {X=811 Y=417}
[ModeInputDispatcher] Lost Move IsInputStart=False Id=0
按下： 0
InkCanvas_OnPointerMoved
执行按下 {X=811 Y=417}
==========InputStart============
Down 811.00,417.00 CurrentInputDictionaryCount=1
执行移动 {X=811 Y=419}
IInputProcessor.Move 811.00,419.00
执行移动 {X=811 Y=420}
执行移动 {X=810 Y=442}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
执行移动 {X=807 Y=453}
[ModeInputDispatcher] MainIdUp MainId=0
SkInkCanvas_StrokesCollected
InputComplete
==========

DrawPath
完成 UNO 绘制
```

```
执行移动 {X=676 Y=406}
执行移动 {X=676 Y=406}
执行移动 {X=676 Y=413}
执行移动 {X=676 Y=416}
执行移动 {X=675 Y=436}
执行移动 {X=673 Y=465}
执行移动 {X=673 Y=473}
执行移动 {X=673 Y=477}
执行移动 {X=672 Y=482}
执行移动 {X=672 Y=485}
执行移动 {X=672 Y=485}
执行移动 {X=672 Y=487}
执行移动 {X=672 Y=487}
执行移动 {X=672 Y=487}
执行移动 {X=672 Y=491}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
执行移动 {X=672 Y=490}
```

```
StartMove0 0.001843ms
完成丢点 0.045932ms
完成绘制 350.179756ms
EndMove0 0.055511ms
```

```
执行移动 {X=304 Y=404} Count=5
丢点数量： 4 实际参与绘制点数：1
执行移动 {X=304 Y=403} Count=2
丢点数量： 1 实际参与绘制点数：1
执行移动 {X=304 Y=402} Count=14
丢点数量： 10 实际参与绘制点数：4
执行移动 {X=305 Y=388} Count=20
丢点数量： 11 实际参与绘制点数：9
执行移动 {X=319 Y=362} Count=39
丢点数量： 24 实际参与绘制点数：15
执行移动 {X=370 Y=341} Count=32
丢点数量： 19 实际参与绘制点数：13
执行移动 {X=411 Y=367} Count=32
丢点数量： 17 实际参与绘制点数：15
执行移动 {X=429 Y=421} Count=38
丢点数量： 19 实际参与绘制点数：19
执行移动 {X=384 Y=471} Count=41
丢点数量： 25 实际参与绘制点数：16
执行移动 {X=352 Y=431} Count=38
丢点数量： 24 实际参与绘制点数：14
执行移动 {X=387 Y=394} Count=33
丢点数量： 18 实际参与绘制点数：15
执行移动 {X=441 Y=398} Count=33
丢点数量： 19 实际参与绘制点数：14
执行移动 {X=476 Y=439} Count=30
丢点数量： 15 实际参与绘制点数：15
执行移动 {X=467 Y=496} Count=37
丢点数量： 17 实际参与绘制点数：20
执行移动 {X=383 Y=531} Count=48
丢点数量： 24 实际参与绘制点数：24
```

```
StartMove0 1.01732ms
完成丢点 2.234721ms
完成绘制 62.223819ms
EndMove0 0.591907ms

StartMove0 0.026032ms
完成丢点 0.023687ms
完成绘制 79.96279ms
EndMove0 0.066013ms

StartMove0 0.028237ms
完成丢点 0.023427ms
完成绘制 81.682518ms
EndMove0 0.064329ms

StartMove0 0.016734ms
完成丢点 0.027576ms
完成绘制 102.057216ms
EndMove0 0.05978ms

StartMove0 0.013667ms
完成丢点 0.026113ms
完成绘制 56.91415ms
EndMove0 0.076975ms

StartMove0 0.007555ms
完成丢点 0.022305ms
完成绘制 60.47936ms
EndMove0 0.070501ms

StartMove0 0.005512ms
完成丢点 0.047134ms
完成绘制 53.710545ms
EndMove0 0.058578ms

StartMove0 0.011724ms
完成丢点 0.016473ms
完成绘制 43.462836ms
EndMove0 0.060321ms

StartMove0 0.010781ms
完成丢点 0.017896ms
完成绘制 32.417868ms
EndMove0 0.080822ms

StartMove0 0.009299ms
完成丢点 0.020642ms
完成绘制 31.7754ms
EndMove0 0.066955ms

StartMove0 0.009839ms
完成丢点 0.017114ms
完成绘制 33.118934ms
EndMove0 0.067155ms

StartMove0 0.007776ms
完成丢点 0.016994ms
完成绘制 22.544749ms
EndMove0 0.121804ms

StartMove0 0.009299ms
完成丢点 0.015311ms
完成绘制 34.547638ms
EndMove0 0.059238ms

StartMove0 0.008497ms
完成丢点 0.013668ms
完成绘制 35.455759ms
EndMove0 0.059419ms

StartMove0 0.010902ms
完成丢点 0.021563ms
完成绘制 34.963833ms
EndMove0 0.074088ms

StartMove0 0.010461ms
完成丢点 0.014529ms
完成绘制 32.985766ms
EndMove0 0.067816ms

StartMove0 0.008177ms
完成丢点 0.011684ms
完成绘制 21.947751ms
EndMove0 0.073307ms

StartMove0 0.007595ms
完成丢点 0.027575ms
完成绘制 33.105206ms
EndMove0 0.058016ms

StartMove0 0.008938ms
完成丢点 0.011723ms
完成绘制 47.611877ms
EndMove0 0.063768ms

StartMove0 0.008778ms
完成丢点 0.016132ms
完成绘制 63.770398ms
EndMove0 0.226354ms

StartMove0 0.086072ms
完成丢点 0.016072ms
完成绘制 70.794826ms
EndMove0 0.056954ms

StartMove0 0.010821ms
完成丢点 0.018357ms
完成绘制 66.856989ms
EndMove0 0.068217ms

StartMove0 0.008437ms
完成丢点 0.01469ms
完成绘制 79.062285ms
EndMove0 0.058377ms

StartMove0 0.003387ms
完成丢点 0.017114ms
完成绘制 80.751992ms
EndMove0 0.057415ms

StartMove0 0.004689ms
完成丢点 0.020921ms
完成绘制 80.268783ms
EndMove0 0.062946ms

StartMove0 0.010421ms
完成丢点 0.023347ms
完成绘制 79.869182ms
EndMove0 0.065211ms
```
