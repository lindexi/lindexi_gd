# dotnetCampus.Ipc

本机内多进程通讯库

| Build | NuGet |
|--|--|
|![](https://github.com/dotnet-campus/dotnetCampus.Ipc/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/dotnetCampus.Ipc.svg)](https://www.nuget.org/packages/dotnetCampus.Ipc)|

开发中……

## 特点

- 采用两个半工命名管道
- 采用 P2P 方式，每个端都是服务端也是客户端
- 加入消息 Ack 机制
- 追求稳定，而不追求高性能

## 功能

- [x] 通讯建立
- [x] 消息收到回复机制
- [ ] 大量异常处理
