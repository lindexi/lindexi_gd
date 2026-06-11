# DotNetCampus.HttpClientOverOpenSsl

让 `HttpClient` 的 HTTPS 请求通过 OpenSSL 原生 DLL（libssl-3）完成 TLS 握手和数据传输。

## 简介

`DotNetCampus.HttpClientOverOpenSsl` 提供了一个基于 `SocketsHttpHandler` + OpenSSL 的 `HttpMessageHandler`，将 HTTPS 请求的 TLS 层交给 OpenSSL 原生库处理，而非依赖 .NET 内置的 `SslStream`。

核心类 `OpenSslSocketsHttpHandler` 继承自 `HttpMessageHandler`，可直接传入 `HttpClient` 构造函数使用：

```csharp
using var handler = new OpenSslSocketsHttpHandler();
using var client = new HttpClient(handler);
var response = await client.GetAsync("https://example.com");
```

## 使用方式

### 1. 安装 NuGet 包

```shell
dotnet add package DotNetCampus.HttpClientOverOpenSsl
```

### 2. 准备 OpenSSL 原生 DLL

本库依赖 OpenSSL 3.x 的原生库文件。你可以选择以下两种方式之一：

#### 方式一：安装 openssl-native NuGet 包（推荐）

```shell
dotnet add package openssl-native
```

此包会自动根据运行时平台（x86 / x64 / arm64）将对应的 DLL 复制到输出目录。

#### 方式二：手动部署 DLL

从 [OpenSSL 官方](https://www.openssl.org/) 或自编译获取以下文件，放置到应用程序的输出目录：

| 平台 | libssl 文件 | libcrypto 文件 |
|------|------------|---------------|
| **Windows x64** | `libssl-3-x64.dll` | `libcrypto-3-x64.dll` |
| **Windows x86** | `libssl-3.dll` | `libcrypto-3.dll` |
| **Windows arm64** | `libssl-3-arm64.dll` | `libcrypto-3-arm64.dll` |

> 库在加载时会按 `AppContext.BaseDirectory` → `runtimes/{rid}/native/` → 自定义 `FallbackLibraryPath` 的顺序查找上述 DLL。如需指定自定义路径，可设置 `OpenSSLNative.FallbackLibraryPath`。

### 3. 在 HttpClient 中使用

```csharp
var handler = new OpenSslSocketsHttpHandler();
var client = new HttpClient(handler);
var html = await client.GetStringAsync("https://www.baidu.com");
```

## 许可证

MIT