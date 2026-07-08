# SlideML 流式片段提取器单元测试设计

> 目标类：`SlideMlFragmentExtractor`（`Streaming/SlideMlFragmentExtractor.cs`）
> 测试框架：MSTest
> 命名规范：`方法名_场景_预期行为`
> 设计目标：覆盖流式追加、完整片段提取、残留缓冲、复杂标签扫描、异常 XML 片段透传等边界行为。

---

## 1. 测试范围

`SlideMlFragmentExtractor` 的核心职责是从 LLM token 流中增量切分出完整的顶层 XML 元素。单元测试应重点验证以下行为：

- `Append` 对输入文本的追加与空值保护。
- `TryExtractFragments` 对 0~N 个完整顶层 XML 片段的提取。
- `TryExtractFragments` 对不完整片段的缓冲保留。
- `GetRemaining` 对残留内容的返回。
- `ScanNextFragment` 间接覆盖复杂 XML 边界：嵌套标签、自闭合标签、注释、处理指令、CDATA、属性引号、非法或不匹配标签。

由于 `ScanNextFragment` 是私有方法，测试不应通过反射直接调用。建议全部通过公开 API：

- `Append(string text)`
- `TryExtractFragments()`
- `GetRemaining()`

---

## 2. 测试类建议

### 2.1 测试文件

建议新增测试文件：

- `Streaming/SlideMlFragmentExtractorTests.cs`

### 2.2 测试类

建议测试类：

- `SlideMlFragmentExtractorTests`

### 2.3 断言重点

每个测试尽量断言以下内容之一：

- 返回片段数量。
- 返回片段文本是否完全一致。
- `GetRemaining()` 是否符合预期。
- 多次调用 `TryExtractFragments()` 是否不会重复返回已消费片段。
- 对不完整输入是否返回空列表并保留原始缓冲。

---

## 3. 构造与基础 API 用例

### 3.1 Append 传入 null

- **用例名**：`Append_NullText_ThrowsArgumentNullException`
- **输入**：`Append(null!)`
- **预期**：抛出 `ArgumentNullException`
- **验证点**：异常类型严格为 `ArgumentNullException`

### 3.2 新实例初始状态

- **用例名**：`GetRemaining_NewInstance_ReturnsEmptyString`
- **输入**：新建 `SlideMlFragmentExtractor`
- **预期**：`GetRemaining()` 返回空字符串
- **验证点**：剩余缓冲为空

### 3.3 无输入时提取

- **用例名**：`TryExtractFragments_NoInput_ReturnsEmptyList`
- **输入**：不调用 `Append`，直接调用 `TryExtractFragments()`
- **预期**：返回空列表
- **验证点**：`Count == 0`，`GetRemaining()` 仍为空

---

## 4. 单个完整片段提取

### 4.1 普通成对标签

- **用例名**：`TryExtractFragments_CompleteElement_ReturnsSingleFragment`
- **输入**：`<Page></Page>`
- **预期**：返回 1 个片段：`<Page></Page>`
- **验证点**：片段文本完全一致，`GetRemaining()` 为空

### 4.2 带文本内容

- **用例名**：`TryExtractFragments_ElementWithText_ReturnsSingleFragment`
- **输入**：`<TextElement>Hello</TextElement>`
- **预期**：返回完整元素
- **验证点**：文本内容不会影响标签深度判断

### 4.3 带属性

- **用例名**：`TryExtractFragments_ElementWithAttributes_ReturnsSingleFragment`
- **输入**：`<Rect Id="r1" Width="100" Height="50"></Rect>`
- **预期**：返回完整元素
- **验证点**：属性不会影响标签名识别

### 4.4 自闭合标签

- **用例名**：`TryExtractFragments_SelfClosingElement_ReturnsSingleFragment`
- **输入**：`<Rect Id="r1"/>`
- **预期**：返回 1 个片段：`<Rect Id="r1"/>`
- **验证点**：自闭合顶层元素可立即提取

### 4.5 自闭合标签斜杠前有空格

- **用例名**：`TryExtractFragments_SelfClosingElementWithSpaceBeforeSlash_ReturnsSingleFragment`
- **输入**：`<Rect Id="r1" />`
- **预期**：返回完整片段
- **验证点**：`/>` 前存在空格时仍识别为自闭合

---

## 5. 多片段连续提取

### 5.1 两个完整片段连续出现

- **用例名**：`TryExtractFragments_TwoCompleteElements_ReturnsTwoFragments`
- **输入**：`<Rect/><TextElement Text="A"/>`
- **预期**：返回 2 个片段，顺序保持不变
- **验证点**：`fragments[0] == "<Rect/>"`，`fragments[1] == "<TextElement Text=\"A\"/>"`

### 5.2 片段之间存在空白

- **用例名**：`TryExtractFragments_ElementsSeparatedByWhitespace_ReturnsFragmentsWithoutWhitespace`
- **输入**：`  <Rect/>\r\n  <TextElement Text="A"/>  `
- **预期**：返回 2 个片段，不包含片段外空白
- **验证点**：前导空白被消费；末尾空白是否残留需要按当前实现确认
- **设计说明**：当前实现会继续扫描末尾空白，并在没有更多片段时返回 `(null, startPos)`，因此末尾空白可能保留在缓冲区。测试应明确记录该行为，避免未来误改。

### 5.3 多次 TryExtractFragments 不重复返回

- **用例名**：`TryExtractFragments_AfterFragmentsConsumed_DoesNotReturnAgain`
- **输入**：先追加 `<Rect/>` 并调用一次 `TryExtractFragments()`，再调用第二次
- **预期**：第一次返回 1 个片段，第二次返回空列表
- **验证点**：已消费内容已从缓冲区移除

---

## 6. 流式增量输入

### 6.1 单个元素分两次追加

- **用例名**：`TryExtractFragments_ElementSplitAcrossAppends_ReturnsAfterComplete`
- **输入**：
  - 第一次追加：`<Page><Rect/>`
  - 第二次追加：`</Page>`
- **预期**：第一次提取返回空；第二次返回 `<Page><Rect/></Page>`
- **验证点**：不完整片段被保留，补齐后可提取

### 6.2 标签名被拆分

- **用例名**：`TryExtractFragments_TagNameSplitAcrossAppends_ReturnsAfterComplete`
- **输入**：
  - 第一次追加：`<Pa`
  - 第二次追加：`ge></Page>`
- **预期**：第一次返回空并保留 `<Pa`；第二次返回 `<Page></Page>`
- **验证点**：不完整开始标签不会被错误丢弃

### 6.3 属性值被拆分

- **用例名**：`TryExtractFragments_AttributeValueSplitAcrossAppends_ReturnsAfterComplete`
- **输入**：
  - 第一次追加：`<Rect Text="hel`
  - 第二次追加：`lo"/>`
- **预期**：第一次返回空；第二次返回完整自闭合标签
- **验证点**：属性引号内的 `>`、`/` 判断不会提前结束

### 6.4 结束标签被拆分

- **用例名**：`TryExtractFragments_EndTagSplitAcrossAppends_ReturnsAfterComplete`
- **输入**：
  - 第一次追加：`<Page><Rect/></Pa`
  - 第二次追加：`ge>`
- **预期**：第一次返回空；第二次返回完整片段
- **验证点**：不完整结束标签会保留

### 6.5 多个 token 拼出多个片段

- **用例名**：`TryExtractFragments_MultipleFragmentsSplitAcrossAppends_ReturnsAvailableFragments`
- **输入**：
  - 第一次追加：`<Rect/><Text`
  - 第一次提取预期：返回 `<Rect/>`，剩余 `<Text`
  - 第二次追加：`Element Text="A"/>`
  - 第二次提取预期：返回 `<TextElement Text="A"/>`
- **验证点**：已完成片段先返回，未完成片段继续保留

---

## 7. 嵌套结构与栈匹配

### 7.1 简单嵌套

- **用例名**：`TryExtractFragments_NestedElements_ReturnsOuterElementOnly`
- **输入**：`<Page><Panel><Rect/></Panel></Page>`
- **预期**：返回 1 个片段，内容为整个 Page
- **验证点**：内部元素不会被作为顶层片段提前返回

### 7.2 多层同名嵌套

- **用例名**：`TryExtractFragments_NestedSameNameElements_ReturnsOuterElement`
- **输入**：`<Panel><Panel><Rect/></Panel></Panel>`
- **预期**：返回整个外层 Panel
- **验证点**：同名标签入栈和出栈顺序正确

### 7.3 兄弟元素嵌套在顶层元素内

- **用例名**：`TryExtractFragments_OuterElementWithSiblingChildren_ReturnsOuterElement`
- **输入**：`<Page><Rect/><TextElement Text="A"/><Image Source="a.png"/></Page>`
- **预期**：返回整个 Page
- **验证点**：多个子元素不会影响顶层闭合判断

### 7.4 标签名包含下划线、点、短横线

- **用例名**：`TryExtractFragments_TagNameWithAllowedSpecialCharacters_ReturnsFragment`
- **输入**：`<a_b.c-d></a_b.c-d>`
- **预期**：返回完整片段
- **验证点**：标签名提取允许 `_`、`.`、`-`

---

## 8. 属性扫描边界

### 8.1 双引号属性中包含大于号

- **用例名**：`TryExtractFragments_DoubleQuotedAttributeContainsGreaterThan_DoesNotEndTagEarly`
- **输入**：`<TextElement Text="1 > 0"/>`
- **预期**：返回完整片段
- **验证点**：属性引号内的 `>` 不会被当作标签结束

### 8.2 单引号属性中包含大于号

- **用例名**：`TryExtractFragments_SingleQuotedAttributeContainsGreaterThan_DoesNotEndTagEarly`
- **输入**：`<TextElement Text='1 > 0'/>`
- **预期**：返回完整片段
- **验证点**：单引号内的 `>` 不会被当作标签结束

### 8.3 属性中包含斜杠

- **用例名**：`TryExtractFragments_AttributeContainsSlash_DoesNotTreatAsSelfClosing`
- **输入**：`<Image Source="http://example.com/a/b.png"></Image>`
- **预期**：返回完整片段
- **验证点**：属性内斜杠不会影响自闭合判断

### 8.4 未闭合双引号属性

- **用例名**：`TryExtractFragments_UnclosedDoubleQuotedAttribute_ReturnsEmptyAndKeepsRemaining`
- **输入**：`<TextElement Text="abc>`
- **预期**：返回空列表，`GetRemaining()` 等于原始输入
- **验证点**：未闭合属性不会被错误提取

### 8.5 未闭合单引号属性

- **用例名**：`TryExtractFragments_UnclosedSingleQuotedAttribute_ReturnsEmptyAndKeepsRemaining`
- **输入**：`<TextElement Text='abc>`
- **预期**：返回空列表，`GetRemaining()` 等于原始输入
- **验证点**：未闭合属性不会被错误提取

---

## 9. 注释、处理指令与 CDATA

### 9.1 顶层元素内部包含注释

- **用例名**：`TryExtractFragments_ElementContainsComment_ReturnsFragment`
- **输入**：`<Page><!-- comment --><Rect/></Page>`
- **预期**：返回完整 Page
- **验证点**：注释内容不会影响深度

### 9.2 注释中包含标签样式文本

- **用例名**：`TryExtractFragments_CommentContainsTagLikeText_IgnoresCommentContent`
- **输入**：`<Page><!-- <Rect></Page> --><Rect/></Page>`
- **预期**：返回完整 Page
- **验证点**：注释中的 `<`、`</` 不参与标签匹配

### 9.3 注释被流式拆分

- **用例名**：`TryExtractFragments_CommentSplitAcrossAppends_ReturnsAfterComplete`
- **输入**：
  - 第一次追加：`<Page><!-- comment`
  - 第二次追加：` --><Rect/></Page>`
- **预期**：第一次返回空；第二次返回完整 Page
- **验证点**：未完成注释保留在缓冲区

### 9.4 处理指令在顶层元素前

- **用例名**：`TryExtractFragments_ProcessingInstructionBeforeElement_ReturnsElement`
- **输入**：`<?xml version="1.0"?><Page></Page>`
- **预期**：返回 `<Page></Page>`
- **验证点**：处理指令不作为片段返回
- **设计说明**：处理指令被跳过并随已消费内容移除。

### 9.5 处理指令在元素内部

- **用例名**：`TryExtractFragments_ElementContainsProcessingInstruction_ReturnsFragment`
- **输入**：`<Page><?slide test?><Rect/></Page>`
- **预期**：返回完整 Page
- **验证点**：处理指令不会影响深度

### 9.6 未完成处理指令

- **用例名**：`TryExtractFragments_IncompleteProcessingInstruction_ReturnsEmptyAndKeepsRemaining`
- **输入**：`<?xml version="1.0"`
- **预期**：返回空列表，缓冲区保留原始输入
- **验证点**：未遇到 `?>` 不消费内容

### 9.7 CDATA 内包含标签样式文本

- **用例名**：`TryExtractFragments_CDataContainsTagLikeText_IgnoresCDataContent`
- **输入**：`<TextElement><![CDATA[<Rect></Page>]]></TextElement>`
- **预期**：返回完整 TextElement
- **验证点**：CDATA 中的标签样式文本不参与匹配

### 9.8 CDATA 被流式拆分

- **用例名**：`TryExtractFragments_CDataSplitAcrossAppends_ReturnsAfterComplete`
- **输入**：
  - 第一次追加：`<TextElement><![CDATA[abc`
  - 第二次追加：`]]></TextElement>`
- **预期**：第一次返回空；第二次返回完整 TextElement
- **验证点**：未完成 CDATA 保留在缓冲区

---

## 10. 不完整输入与残留缓冲

### 10.1 只有小于号

- **用例名**：`TryExtractFragments_OnlyLessThan_ReturnsEmptyAndKeepsRemaining`
- **输入**：`<`
- **预期**：返回空列表，剩余 `<`
- **验证点**：无法判断后续字符时不消费

### 10.2 不完整开始标签

- **用例名**：`TryExtractFragments_IncompleteStartTag_ReturnsEmptyAndKeepsRemaining`
- **输入**：`<Page`
- **预期**：返回空列表，剩余 `<Page`
- **验证点**：未遇到 `>` 不消费

### 10.3 不完整结束标签

- **用例名**：`TryExtractFragments_IncompleteEndTag_ReturnsEmptyAndKeepsRemaining`
- **输入**：`<Page></Pa`
- **预期**：返回空列表，剩余完整输入
- **验证点**：顶层元素尚未闭合时不返回片段

### 10.4 元素未闭合

- **用例名**：`TryExtractFragments_UnclosedElement_ReturnsEmptyAndKeepsRemaining`
- **输入**：`<Page><Rect/>`
- **预期**：返回空列表，剩余完整输入
- **验证点**：扫描结束但深度未归零时不消费

### 10.5 空白加不完整片段

- **用例名**：`TryExtractFragments_LeadingWhitespaceBeforeIncompleteElement_KeepsAllRemaining`
- **输入**：`  <Page>`
- **预期**：返回空列表，剩余内容仍包含前导空白
- **验证点**：在无法形成完整片段时不应仅消费前导空白

---

## 11. 非片段噪声与前导内容

### 11.1 前导自然语言文本

- **用例名**：`TryExtractFragments_TextBeforeElement_SkipsTextAndReturnsElement`
- **输入**：`下面是生成内容：<Page></Page>`
- **预期**：返回 `<Page></Page>`
- **验证点**：顶层元素前的非 XML 文本会被跳过并消费

### 11.2 片段之间存在自然语言文本

- **用例名**：`TryExtractFragments_TextBetweenElements_ReturnsBothElements`
- **输入**：`<Rect/>说明文字<TextElement Text="A"/>`
- **预期**：返回 `<Rect/>` 和 `<TextElement Text="A"/>`
- **验证点**：两个顶层片段之间的噪声文本不会阻止后续提取

### 11.3 只有自然语言文本

- **用例名**：`TryExtractFragments_TextWithoutElement_ReturnsEmptyAndMayKeepRemaining`
- **输入**：`这里没有 XML`
- **预期**：返回空列表
- **验证点**：明确当前实现对纯文本的缓冲策略
- **设计说明**：当前扫描结束后返回 `(null, startPos)`，因此纯文本会保留在缓冲区。该行为可能导致长期无 XML 输入时缓冲增长，测试用于锁定现状或暴露后续优化点。

### 11.4 非法小于号后跟普通字符

- **用例名**：`TryExtractFragments_InvalidLessThanSequenceBeforeElement_SkipsInvalidSequenceAndReturnsElement`
- **输入**：`<1 invalid><Page></Page>`
- **预期**：返回 `<Page></Page>`
- **验证点**：无法识别的 `<` 场景会跳过，不会导致死循环

---

## 12. 不匹配标签与容错行为

### 12.1 内层结束标签不匹配

- **用例名**：`TryExtractFragments_MismatchedInnerEndTag_ReturnsInvalidFragmentForParser`
- **输入**：`<Page><Panel></Rect>`
- **预期**：返回 1 个片段：`<Page><Panel></Rect>`
- **验证点**：片段内部遇到不匹配结束标签时，返回非法片段供上层 XML 解析报错

### 12.2 顶层结束标签不匹配

- **用例名**：`TryExtractFragments_MismatchedTopLevelEndTag_ReturnsInvalidFragmentForParser`
- **输入**：`<Page></Panel>`
- **预期**：返回 1 个片段：`<Page></Panel>`
- **验证点**：不匹配结束标签不被吞掉，应形成可交给解析器处理的非法片段

### 12.3 嵌套中间标签未闭合但外层闭合

- **用例名**：`TryExtractFragments_OuterEndTagAutoPopsUnclosedInnerTags_ReturnsOuterFragment`
- **输入**：`<Page><Panel><Rect></Page>`
- **预期**：返回 `<Page><Panel><Rect></Page>`
- **验证点**：遇到外层结束标签时，会弹出中间未闭合标签和匹配标签
- **设计说明**：这是当前栈搜索与自动弹栈逻辑的重要容错行为，应单独锁定。

### 12.4 只有结束标签

- **用例名**：`TryExtractFragments_OnlyEndTag_ReturnsEmptyAndConsumesOrKeepsByCurrentBehavior`
- **输入**：`</Page>`
- **预期**：返回空列表
- **验证点**：明确当前实现对孤立结束标签的处理策略
- **设计说明**：当前代码在 `depth == 0` 且无 `fragmentStart` 时会跳过结束标签并最终保留缓冲，测试应记录实际行为，必要时可作为后续修复依据。

---

## 13. 消费位置与残留组合场景

### 13.1 完整片段后跟不完整片段

- **用例名**：`TryExtractFragments_CompleteThenIncomplete_ReturnsCompleteAndKeepsIncomplete`
- **输入**：`<Rect/><Page>`
- **预期**：返回 `<Rect/>`，剩余 `<Page>`
- **验证点**：已完成片段被消费，不完整片段保留

### 13.2 完整片段后跟半个小于号

- **用例名**：`TryExtractFragments_CompleteThenTrailingLessThan_ReturnsCompleteAndKeepsLessThan`
- **输入**：`<Rect/><`
- **预期**：返回 `<Rect/>`，剩余 `<`
- **验证点**：尾部半个标签头不丢失

### 13.3 不完整片段补齐后继续提取后续完整片段

- **用例名**：`TryExtractFragments_IncompleteThenCompletedWithNextFragment_ReturnsBothInOrder`
- **输入**：
  - 第一次追加：`<Page><Rect/>`
  - 第二次追加：`</Page><TextElement Text="A"/>`
- **预期**：第二次返回 `<Page><Rect/></Page>` 和 `<TextElement Text="A"/>`
- **验证点**：补齐前一片段后能继续扫描后续片段

### 13.4 片段消费后剩余空白

- **用例名**：`TryExtractFragments_FragmentFollowedByWhitespace_ReturnsFragmentAndRecordsWhitespaceBehavior`
- **输入**：`<Rect/>   `
- **预期**：返回 `<Rect/>`
- **验证点**：确认 `GetRemaining()` 对尾随空白的实际结果
- **设计说明**：该行为与扫描循环的 `consumed` 更新有关，建议测试锁定，防止未来修改导致流式状态异常。

---

## 14. 大小写与标签名匹配

### 14.1 开始与结束标签大小写不同

- **用例名**：`TryExtractFragments_EndTagDifferentCase_ReturnsFragment`
- **输入**：`<Page></page>`
- **预期**：返回 `<Page></page>`
- **验证点**：结束标签匹配使用大小写不敏感比较

### 14.2 嵌套标签大小写不同

- **用例名**：`TryExtractFragments_NestedEndTagDifferentCase_ReturnsOuterFragment`
- **输入**：`<Page><Panel></panel></PAGE>`
- **预期**：返回整个 Page
- **验证点**：嵌套栈匹配同样大小写不敏感

---

## 15. XML 特殊结构边界

### 15.1 DOCTYPE 样式内容

- **用例名**：`TryExtractFragments_DoctypeBeforeElement_SkipsUnsupportedDeclarationAndReturnsElement`
- **输入**：`<!DOCTYPE Page><Page></Page>`
- **预期**：返回 `<Page></Page>`
- **验证点**：非注释、非 CDATA 的 `<!...>` 不会导致死循环
- **设计说明**：当前实现不专门支持 DOCTYPE，只会按其他 `<` 情况跳过字符。该测试用于锁定不会阻塞后续元素提取。

### 15.2 顶层 CDATA 后跟元素

- **用例名**：`TryExtractFragments_TopLevelCDataBeforeElement_SkipsCDataAndReturnsElement`
- **输入**：`<![CDATA[text]]><Page></Page>`
- **预期**：返回 `<Page></Page>`
- **验证点**：顶层 CDATA 不作为片段返回

### 15.3 XML 实体文本

- **用例名**：`TryExtractFragments_ElementContainsXmlEntityText_ReturnsFragment`
- **输入**：`<TextElement Text="A &amp; B"></TextElement>`
- **预期**：返回完整片段
- **验证点**：实体文本不影响扫描

---

## 16. 建议优先级

### 16.1 第一优先级：必须覆盖

这些用例直接关系到流式输出是否可用：

- `TryExtractFragments_CompleteElement_ReturnsSingleFragment`
- `TryExtractFragments_SelfClosingElement_ReturnsSingleFragment`
- `TryExtractFragments_TwoCompleteElements_ReturnsTwoFragments`
- `TryExtractFragments_ElementSplitAcrossAppends_ReturnsAfterComplete`
- `TryExtractFragments_CompleteThenIncomplete_ReturnsCompleteAndKeepsIncomplete`
- `TryExtractFragments_NestedElements_ReturnsOuterElementOnly`
- `TryExtractFragments_UnclosedElement_ReturnsEmptyAndKeepsRemaining`
- `TryExtractFragments_DoubleQuotedAttributeContainsGreaterThan_DoesNotEndTagEarly`
- `TryExtractFragments_ElementContainsComment_ReturnsFragment`
- `TryExtractFragments_CDataContainsTagLikeText_IgnoresCDataContent`

### 16.2 第二优先级：复杂边界

这些用例用于保护复杂扫描逻辑：

- `TryExtractFragments_TagNameSplitAcrossAppends_ReturnsAfterComplete`
- `TryExtractFragments_AttributeValueSplitAcrossAppends_ReturnsAfterComplete`
- `TryExtractFragments_EndTagSplitAcrossAppends_ReturnsAfterComplete`
- `TryExtractFragments_NestedSameNameElements_ReturnsOuterElement`
- `TryExtractFragments_CommentSplitAcrossAppends_ReturnsAfterComplete`
- `TryExtractFragments_CDataSplitAcrossAppends_ReturnsAfterComplete`
- `TryExtractFragments_EndTagDifferentCase_ReturnsFragment`
- `TryExtractFragments_OuterEndTagAutoPopsUnclosedInnerTags_ReturnsOuterFragment`

### 16.3 第三优先级：行为记录与后续优化依据

这些用例主要用于记录当前行为，避免未来重构时出现隐性变更：

- `TryExtractFragments_TextWithoutElement_ReturnsEmptyAndMayKeepRemaining`
- `TryExtractFragments_OnlyEndTag_ReturnsEmptyAndConsumesOrKeepsByCurrentBehavior`
- `TryExtractFragments_FragmentFollowedByWhitespace_ReturnsFragmentAndRecordsWhitespaceBehavior`
- `TryExtractFragments_DoctypeBeforeElement_SkipsUnsupportedDeclarationAndReturnsElement`

---

## 17. 测试实现注意事项

- 不建议直接测试私有方法 `ScanNextFragment`，应通过公开 API 间接覆盖。
- 断言片段文本时应使用完全匹配，确保提取边界没有多吃或少吃字符。
- 流式拆分场景应在每次 `Append` 后立即调用 `TryExtractFragments()`，模拟真实 token 流。
- 每个测试使用新的 `SlideMlFragmentExtractor` 实例，避免缓冲状态互相污染。
- 对当前实现存在争议的行为，应在测试名或断言消息中说明是在锁定当前行为，而不是声明这是理想设计。
- 若后续决定调整纯文本、尾随空白、孤立结束标签的消费策略，应同步更新这些行为记录型测试。

---

## 18. 后续可考虑的参数化测试

以下场景适合用 `[DataTestMethod]` 与 `[DataRow]` 合并：

### 18.1 不完整输入

- `<`
- `<Page`
- `<Page>`
- `<Page></Pa`
- `<TextElement Text="abc>`
- `<TextElement Text='abc>`

统一预期：返回空列表，并保留输入。

### 18.2 自闭合标签

- `<Rect/>`
- `<Rect />`
- `<TextElement Text="A"/>`
- `<Image Source="a.png" />`

统一预期：返回单个完整片段。

### 18.3 大小写不敏感结束标签

- `<Page></page>`
- `<Panel></PANEL>`
- `<a_b.c-d></A_B.C-D>`

统一预期：返回单个完整片段。

---

## 19. 验收标准

完成测试后，应能证明：

- 完整 XML 顶层元素可以被稳定提取。
- 不完整 XML 片段不会被提前消费或错误返回。
- 多个片段可以按顺序一次性返回。
- 流式拆分输入在补齐后可以继续正确提取。
- 注释、处理指令、CDATA、属性引号不会破坏扫描状态。
- 不匹配标签的容错行为被明确记录。
- 缓冲区消费与残留行为被测试覆盖，便于未来安全重构 `ScanNextFragment`。
