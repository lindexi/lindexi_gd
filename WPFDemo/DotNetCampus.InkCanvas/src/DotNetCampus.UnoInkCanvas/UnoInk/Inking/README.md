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
- [x] 落笔的点被丢失没有参与渲染
- [x] 第一笔落笔慢
  - 也许是界面输出的原因 8f2c35724079e00adf2b1a216392dd4f1bb70f0b
  - 也是因为控制台输出
- [x] 多指的支持
  - 使用 https://github.com/unoplatform/uno/pull/15799 代码
  - 使用没合入主分支的 https://github.com/lindexi/uno/ 579d4d502786ac9344e20f07f1800a4672c880df 代码
- [ ] 窗口背景透明
  - 使用 https://github.com/unoplatform/uno/pull/16956 合入等发布
- [x] 快速写笔迹会闪烁
  - 大量压入导致曝光没有处理
  - 修复笔迹转换动态
- [x] 启动时会闪烁窗口
  - 去掉 SetOwner 关系，尝试断开两个窗口关系测试是否快速写时闪烁。依然闪烁 013a257942a6e840a9d09547aaf955f0382ce83d
  - 测试是否动态笔迹层转换静态笔迹层的问题
  - 调用时间到全屏时间十分短，因此不是 XMapWindow 到 TryEnterFullScreenMode 的耗时
  - 测试窗口启动时间
  - 经过测试窗口启动时间是设置窗口图标耗时在 UNO 的 X11XamlRootHost.UpdateWindowPropertiesFromPackage 的耗时
    - 在 9f92656ee64eb5fc0701848931fd3d914a31b233 设置图标是空跳过
- [ ] 动态笔迹重新命名为 DynamicRenderer 类型，保持和 WPF 相同
- [ ] 考虑笔迹在所有元素之上
- [ ] 优化抬笔闪烁
  - 核心原因是渲染同步
  - 尝试使用还没合入的方法 没有用
  - 加上项目引用 67fd7c3106472b8f0a84d3c4f67387aec391e03a
  - 测试 SkiaVisual 是否每一笔都是重新绘制
    - 经过测试每一次都是重新绘制清空画布重新绘制 a3193333714925764bbd3a5e7c362b2bbc5332b6
  - [x] 落笔 SkiaVisual 就会消失不可见
  - [x] 设置全局使用 `_locker 作为锁
  - 使用 SkiaVisual 依然存在闪烁
    - 经过实际测试 SkiaVisual 不如原本的 SKXamlCanvas 的方式。原因是 SkiaVisual 也无法解决闪烁问题，依然需要等待下一次才能渲染，只是等待时间稍微短一点而已，依然存在闪烁。且 SkiaVisual 是清空画布的，需要每次都绘制所有的静态笔迹。也就是说笔迹数量越多，绘制时间越长，越卡
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
- [ ] 如何从 XIEnterLeaveEvent 获取 Id 号
- [ ] 多指抬起静态笔迹效果错误
- 多指下，单指抬起，单指成为静态笔迹层。此时绘制层依然记录错误信息

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

## 窗口启动闪烁测试

```
OnLaunched 时间 520568078 Thread=5
Show Window     520568238 Thread=5  520568238-520568078=160
窗口构造函数    520568406 Thread=5  520568406-520568238=168
全屏时间        520568410 Thread=5  520568410-520568406=4
```

测试代码:

- 仓库： https://github.com/lindexi/uno/
- 代码： 3c850a1c4ad7e56e572c10b4242f94c450fbe430

```
OnLaunched 时间      520860590 Thread=5
Show Window          520860738 Thread=5
UpdateWindow         520860870 Thread=5 520860870-520860738=132 性能核心
X11XamlRootHost 完成 520860874 Thread=5 520860874-520860870=4
窗口构造函数         520860918 Thread=5 窗口构造函数-UpdateWindow=520860918-520860870=48
全屏时间 520860922 Thread=5
```

- 代码： 59c036f79186c3634955d70a424c0b9b5ef9fb34

```
OnLaunched 时间     521364592 Thread=5
Show Window         521364724 Thread=5
StartUpdateWindow   521364756 Thread=5 521364756-521364724=32
EndUpdateWindow     521364848 Thread=5 521364848-521364756=92
X11XamlRootHost 完成 521364860 Thread=5
窗口构造函数 521364892 Thread=5
全屏时间 521364896 Thread=5
```

```
9f92656ee64eb5fc0701848931fd3d914a31b233

OnLaunched 时间 521763938 Thread=5
Show Window 521764070 Thread=5
StartUpdateWindow 521764098 Thread=5
EndUpdateWindow   521764102 Thread=5 521764102-521764098=4
X11XamlRootHost 完成 521764106 Thread=5 521764106-521764070=36
窗口构造函数 521764150 Thread=5
全屏时间 521764154 Thread=5  521764154-521764070=84
```
