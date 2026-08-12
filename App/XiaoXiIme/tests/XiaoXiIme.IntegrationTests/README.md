# XiaoXiIme.IntegrationTests

小希输入法系统级集成与冒烟测试项目。托管测试覆盖 Host IPC 基础闭环、Host/UI 状态查询、候选窗状态映射、显示/隐藏、分页、高亮和锚点占位。自包含 `XiaoXiIme.IntegrationTestHost` 还会在管理员沙箱或可还原 VM 中执行传统 IME 的真实安装、HKL 激活、HIMC 打开、`SendInput` 自动注入 `xx` 和 EDIT 精确上屏“小希”验证；该破坏性流程由 CLI 的 `integration-run --skip-tsf` 调用，不属于普通 `dotnet test`。
