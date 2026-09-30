# XiaoXiIme.ImeCore 小希输入法核心逻辑项目。职责：输入状态机、组合串管理、候选协议抽象、提交、取消、分页、候选选择以及不改变当前组合的用户词注册/注销。`QueryConversionList` 按源串查询候选，`QueryReverseConversionList` 按文本反查规范 reading，二者都不改变当前组合。该项目保持纯逻辑，便于单元测试。
