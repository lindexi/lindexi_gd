# dotnetCampus.Ipc

本机内多进程通讯库

| Build | NuGet |
|--|--|
|![](https://github.com/dotnet-campus/dotnetCampus.Ipc/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/dotnetCampus.Ipc.svg)](https://www.nuget.org/packages/dotnetCampus.Ipc)|

开发中……

大概是底层基础可用

## 特点

- 采用两个半工命名管道
- 采用 P2P 方式，每个端都是服务端也是客户端
- 加入消息 Ack 机制
- 提供 PeerProxy 机制，利用这个机制可以进行发送和接收某个对方的信息
- 追求稳定，而不追求高性能

## 功能

- [x] 通讯建立
- [x] 消息收到回复机制
- [ ] 大量异常处理

## 项目设计

### dotnetCampus.Ipc.Abstractions

提供可替换的抽象实现，只是有接口等

进度：等待设计 API 中，设计的时候需要考虑不同的底层技术需要的支持

应该分为两层的 API 实现，第一层为底层 API 用于给各个底层基础项目所使用。第二层是顶层 API 用于给上层业务开发者使用

### dotnetCampus.Ipc.PipeCore

提供对 dotnetCampus.Ipc.Abstractions 的部分实现

使用管道制作的 IPC 通讯

不直接面向最终开发者，或者说只有很少的类会被开发者使用到，这个项目的 API 不做设计，注重的是提供稳定的管道进程间通讯方式

特点是有很多很底层的 API 开放，以及用起来的时候需要了解管道的知识

进度：基本可用

### dotnetCampus.Ipc

提供给上层业务开发者使用的项目，这个项目有良好的 API 设计

也许只有在初始化的时候，才需要用到少量的 dotnetCampus.Ipc.PipeCore 项目里面管道的知识

这个项目能支持的不应该只有管道一个方式，而是任何基于 dotnetCampus.Ipc.Abstractions 的抽象实现都应该支持

进度：等待 API 设计中，也许会接入 [https://github.com/jacqueskang/IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework) 的实现，或者模拟 WCF 或 Remoting 的实现

## 进度

- 基本完成 dotnetCampus.Ipc.PipeCore 部分
- 最小可用呆魔