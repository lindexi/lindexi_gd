# 代码框架

![](./Docs/Image/Image1.png)


1. 从 UNO 接收输入，在 X11 绘制
   - 开发 01781d31ae7c06e094dabfb6f80d81a0877bd8ef t/lindexi/Ink 可以切分支
2. 从 UNO 接收输入，在 UNO 绘制，在 X11 显示
   - 可基于 42cfb47f083c60c0ec37f30be10d2ed945ae1123 重新开发
3. 从 X11 接收输入，在 X11 绘制，判断 UNO 命中
   - 去掉 ddad66981bd175e1ef84bd3bd547dc329964981c 即可使用，效果很好

- [x] 落笔时闪烁
  - 尝试解决 df33f57b3c74a331e6651cb44dfdb4ee351f496c 无效
  - 尝试修复按下的点被记录，在移动被错误读取 9e4fecc95ecc7cc2b2fe62f697332d1883e0a1c3 无效
  - [x] 尝试将上层发送的点和底层接收到的点记录起来 看起来点似乎正常
  - [x] 测试 SKPathFillType 的行为 完成测试，不会出现空的情况
  - [x] 尝试修改支持多个点的输入
    - 笔迹点多了会卡
    - 依然落笔卡顿和闪烁
  - 使用其他的笔的实现
- [ ] 第一笔落笔慢
- [ ] 多指的支持
- [ ] 窗口背景透明
  - https://github.com/unoplatform/uno/pull/16956
- [x] 快速写笔迹会闪烁
  - 大量压入导致曝光没有处理
  - 修复笔迹转换动态
- [ ] 启动时会闪烁窗口
  - 去掉 SetOwner 关系，尝试断开两个窗口关系测试是否快速写时闪烁。依然闪烁 013a257942a6e840a9d09547aaf955f0382ce83d
  - 测试是否动态笔迹层转换静态笔迹层的问题
- [ ] 动态笔迹重新命名为 DynamicRenderer 类型，保持和 WPF 相同
- [ ] 考虑笔迹在所有元素之上
- [ ] 优化抬笔闪烁
  - 核心原因是渲染同步
  - 尝试使用还没合入的方法
  - 加上项目引用 67fd7c3106472b8f0a84d3c4f67387aec391e03a
  - 测试 SkiaVisual 是否每一笔都是重新绘制
    - 经过测试每一次都是重新绘制清空画布重新绘制 a3193333714925764bbd3a5e7c362b2bbc5332b6
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
重新输出绘制的点的数量，用于测试点的性能
丢点数量： 4 实际参与绘制点数：3
StartMove0 0.975177ms
完成丢点 2.247009ms
完成绘制 86.019001ms / 3 = 28.6666666667ms
EndMove0 0.559283ms

丢点数量： 12 实际参与绘制点数：7
StartMove0 0.016012ms
完成丢点 0.025371ms
完成绘制 132.563285ms = 18.9376121429
EndMove0 0.227597ms

丢点数量： 23 实际参与绘制点数：18
StartMove0 0.008097ms
完成丢点 0.04004ms
完成绘制 246.280892ms = 13.6822717778
EndMove0 0.086533ms

丢点数量： 29 实际参与绘制点数：16
StartMove0 0.004128ms
完成丢点 0.036273ms
完成绘制 199.30446ms = 12.45652875
EndMove0 0.060361ms

丢点数量： 24 实际参与绘制点数：12
StartMove0 0.003608ms
完成丢点 0.026754ms
完成绘制 148.916062ms = 12.4096718333
EndMove0 0.05988ms

丢点数量： 18 实际参与绘制点数：10
StartMove0 0.003507ms
完成丢点 0.046292ms
完成绘制 132.916594ms = 13
EndMove0 0.247497ms

丢点数量： 17 实际参与绘制点数：9
StartMove0 0.109179ms
完成丢点 0.022084ms
完成绘制 112.26427ms
EndMove0 0.072746ms

丢点数量： 13 实际参与绘制点数：8
StartMove0 0.003407ms
完成丢点 0.026513ms
完成绘制 110.956024ms
EndMove0 0.057976ms

丢点数量： 12 实际参与绘制点数：9
StartMove0 0.009619ms
完成丢点 0.02497ms
完成绘制 113.96889ms
EndMove0 0.065772ms

丢点数量： 10 实际参与绘制点数：11
StartMove0 0.00521ms
完成丢点 0.021122ms
完成绘制 135.196309ms
EndMove0 0.086353ms

丢点数量： 14 实际参与绘制点数：10
StartMove0 0.003427ms
完成丢点 0.026133ms
完成绘制 137.667408ms
EndMove0 0.05497ms

丢点数量： 13 实际参与绘制点数：12
StartMove0 0.00525ms
完成丢点 0.040902ms
完成绘制 157.460748ms
EndMove0 0.062205ms

丢点数量： 14 实际参与绘制点数：15
StartMove0 0.015711ms
完成丢点 0.023648ms
完成绘制 211.317066ms
EndMove0 0.059058ms

丢点数量： 17 实际参与绘制点数：20
StartMove0 0.019299ms
完成丢点 0.03483ms
完成绘制 270.602439ms
EndMove0 0.055812ms

丢点数量： 24 实际参与绘制点数：26
StartMove0 0.005812ms
完成丢点 0.037676ms
完成绘制 363.360363ms
EndMove0 0.056433ms

丢点数量： 33 实际参与绘制点数：34
StartMove0 0.007495ms
完成丢点 0.048858ms
完成绘制 437.889946ms
EndMove0 0.054329ms

丢点数量： 42 实际参与绘制点数：42
StartMove0 0.006372ms
完成丢点 0.047355ms
完成绘制 553.397704ms
EndMove0 0.055411ms
```

使用裁剪笔迹的性能

```
丢点数量： 5 实际参与绘制点数：1
StartMove0 0.905957ms
完成丢点 2.393861ms
完成绘制 7.398419ms
EndMove0 0.583831ms

丢点数量： 2 实际参与绘制点数：1
StartMove0 0.01517ms
完成丢点 0.016773ms
完成绘制 37.937629ms
EndMove0 0.084369ms

丢点数量： 11 实际参与绘制点数：2
StartMove0 0.012304ms
完成丢点 0.016172ms
完成绘制 5.191191ms
EndMove0 0.252567ms

丢点数量： 16 实际参与绘制点数：4
StartMove0 0.021984ms
完成丢点 0.026894ms
完成绘制 1.021749ms
EndMove0 0.050401ms

丢点数量： 7 实际参与绘制点数：2
StartMove0 0.002505ms
完成丢点 0.008677ms
完成绘制 0.439221ms
EndMove0 0.028257ms

丢点数量： 3 实际参与绘制点数：2
StartMove0 0.002465ms
完成丢点 0.005231ms
完成绘制 0.535514ms
EndMove0 0.032084ms

StartMove0 0.006473ms
EndMove0 0.365874ms

StartMove0 0.011744ms
EndMove0 0.431345ms

StartMove0 0.008457ms
EndMove0 0.442788ms

StartMove0 0.012284ms
EndMove0 0.577458ms

StartMove0 0.008777ms
EndMove0 0.462207ms

StartMove0 0.004429ms
EndMove0 0.401665ms

StartMove0 0.010161ms
EndMove0 0.465113ms

StartMove0 0.006453ms
EndMove0 0.401405ms

StartMove0 0.006934ms
EndMove0 0.489562ms

StartMove0 0.005271ms
EndMove0 0.45401ms

StartMove0 0.009639ms
EndMove0 0.585735ms

StartMove0 0.007535ms
EndMove0 0.450563ms

StartMove0 0.013527ms
EndMove0 0.749584ms

StartMove0 0.008738ms
EndMove0 0.562428ms

StartMove0 0.00511ms
EndMove0 0.44401ms

StartMove0 0.01032ms
EndMove0 0.746658ms

StartMove0 0.009138ms
EndMove0 0.632649ms

StartMove0 0.005611ms
EndMove0 0.594111ms

StartMove0 0.009099ms
EndMove0 0.574232ms

StartMove0 0.009399ms
EndMove0 0.549883ms

StartMove0 0.007234ms
EndMove0 0.661487ms

StartMove0 0.007776ms
EndMove0 0.576656ms

StartMove0 0.029018ms
EndMove0 1.398846ms

StartMove0 0.005832ms
EndMove0 0.631667ms

StartMove0 0.018758ms
EndMove0 0.725756ms

StartMove0 0.008898ms
EndMove0 0.672188ms

StartMove0 0.007815ms
EndMove0 0.936238ms

StartMove0 0.022345ms
EndMove0 1.014896ms

StartMove0 0.017315ms
EndMove0 0.781768ms

StartMove0 0.005311ms
EndMove0 0.679583ms

StartMove0 0.015371ms
EndMove0 1.033273ms

StartMove0 0.015411ms
EndMove0 0.84311ms

StartMove0 0.007976ms
EndMove0 0.730204ms

StartMove0 0.018336ms
EndMove0 0.915236ms

StartMove0 0.018237ms
EndMove0 0.901688ms

StartMove0 0.015711ms
EndMove0 0.844353ms

StartMove0 0.003728ms
EndMove0 0.68279ms

StartMove0 0.017956ms
EndMove0 0.924614ms

StartMove0 0.021343ms
EndMove0 0.939244ms

StartMove0 0.007856ms
EndMove0 0.91269ms

StartMove0 0.023287ms
EndMove0 1.053493ms

StartMove0 0.021443ms
EndMove0 0.998402ms

StartMove0 0.00501ms
EndMove0 0.954093ms

StartMove0 0.031763ms
EndMove0 1.330248ms

丢点数量： 3 实际参与绘制点数：1
StartMove0 0.00501ms
完成丢点 0.011843ms
完成绘制 0.794814ms
EndMove0 0.030462ms

丢点数量： 1 实际参与绘制点数：1
StartMove0 0.003608ms
完成丢点 0.007114ms
完成绘制 0.826177ms
EndMove0 0.030962ms

StartMove0 0.014389ms
EndMove0 1.313595ms

StartMove0 0.007535ms
EndMove0 0.946799ms

StartMove0 0.005972ms
EndMove0 0.949844ms

StartMove0 0.018077ms
EndMove0 1.197061ms

StartMove0 0.004188ms
EndMove0 0.968582ms

StartMove0 0.0099ms
EndMove0 1.183233ms

StartMove0 0.02004ms
EndMove0 1.126359ms

StartMove0 0.006312ms
EndMove0 0.919664ms

StartMove0 0.003748ms
EndMove0 0.852911ms

StartMove0 0.005211ms
EndMove0 0.933692ms

StartMove0 0.01042ms
EndMove0 1.025196ms

StartMove0 0.003247ms
EndMove0 0.986899ms

StartMove0 0.012625ms
EndMove0 1.055798ms

StartMove0 0.023608ms
EndMove0 1.293173ms

StartMove0 0.003567ms
EndMove0 0.929324ms

StartMove0 0.024609ms
EndMove0 1.129305ms

StartMove0 0.016493ms
EndMove0 1.123032ms

StartMove0 0.012745ms
EndMove0 0.966338ms

StartMove0 0.005611ms
EndMove0 0.948342ms

StartMove0 0.006092ms
EndMove0 1.013012ms

StartMove0 0.00467ms
EndMove0 0.909965ms

StartMove0 0.006934ms
EndMove0 1.001108ms

StartMove0 0.007355ms
EndMove0 1.269085ms

StartMove0 0.008837ms
EndMove0 1.054415ms

StartMove0 0.003748ms
EndMove0 0.952711ms

StartMove0 0.008397ms
EndMove0 1.055798ms

StartMove0 0.023787ms
EndMove0 1.050106ms

StartMove0 0.004369ms
EndMove0 0.86762ms

StartMove0 0.006573ms
EndMove0 0.853051ms

StartMove0 0.00495ms
EndMove0 0.940346ms

StartMove0 0.004669ms
EndMove0 0.918482ms

StartMove0 0.007775ms
EndMove0 2.045222ms

StartMove0 0.007395ms
EndMove0 1.090908ms

StartMove0 0.017175ms
EndMove0 1.321591ms
```
