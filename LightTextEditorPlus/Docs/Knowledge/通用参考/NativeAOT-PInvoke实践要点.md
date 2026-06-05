# NativeAOT PInvoke 实践要点

> 从任务日志 `2026-02-28-NativeAot-RunProperty导出与PInvoke补充` 和 `领域与技术参考.md` §5 提炼。

---

## 核心限制

### `UnmanagedCallersOnly` 禁止 `out`/`ref`/`in` 参数

标记 `[UnmanagedCallersOnly]` 的方法被 AOT 工具链约束为仅接受简单类型参数。

```csharp
// 错误：out/ref 参数
[UnmanagedCallersOnly(EntryPoint = "get_font_size")]
public static float GetFontSize(IntPtr handle, out int errorCode) { ... }

// 正确：改用指针输出
[UnmanagedCallersOnly(EntryPoint = "get_font_size")]
public static void GetFontSize(IntPtr handle, float* result) { *result = value; }
```

### 错误传递策略

- 不通过异常在托管/非托管边界传递错误
- 统一使用 `ErrorCode` 枚举返回值
- 托管侧封装成 `out` 参数或 `TryGet` 模式

---

## 字符串跨边界传递

### 两段式读取模式

```csharp
// 第一遍：传 null 获取长度
int length = Native.GetStringLength(handle, null);
// 第二遍：分配缓冲区并读取
byte[] buffer = new byte[length * 2];
fixed (byte* p = buffer) { Native.GetStringLength(handle, p); }
string result = Encoding.Unicode.GetString(buffer);
```

---

## 常见陷阱

| 陷阱 | 说明 |
|---|---|
| `UnmanagedCallersOnly` 签名 | 不能使用 `out`/`ref`/`in`，改用指针 |
| 跨边界异常 | 不应让托管异常越过 PInvoke 边界 |
| 字符串长度 | 先获取长度再分配，避免缓冲区溢出 |
| 泛型导出 | NativeAOT 不支持泛型导出函数 |
