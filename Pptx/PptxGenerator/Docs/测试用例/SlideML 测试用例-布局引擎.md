# SlideML 布局引擎单元测试用例

> 目标类：`SlideMlLayoutEngine`（`Rendering/SlideMlLayoutEngine.cs`）
> 测试框架：MSTest
> 命名规范：`方法名_场景_预期行为`
> 说明：标有「[已有]」的为现有测试的用例，此处列出以便查阅完整性；其余的为建议新增用例。

---

## 1. PreLayout — 绝对定位

### 1.1 [已有] 基本绝对定位元素坐标
- **用例名**: `AbsoluteLayout_BehaviorUnchanged`
- **输入**: Panel(Absolute) + Rect(X=50,Y=30,W=100,H=50) + Rect(X=200,Y=100,W=150,H=80)
- **预期**: r1.LayoutBounds=(50,30,100,50), r2.LayoutBounds=(200,100,150,80)
- **验证点**: X、Y、Width、Height 与声明一致

### 1.2 Panel 自动尺寸（未指定 Width/Height）
- **用例名**: `PreLayout_AbsolutePanel_AutoSize_WrapsContent`
- **输入**: Panel(Absolute, 无 Width/Height) + Rect(X=10,Y=10,W=100,H=50) + Rect(X=150,Y=30,W=200,H=80)
- **预期**: Panel.ActualWidth = 10+100 (无 padding，contentRight=10+100=110。但注意：实际逻辑是第一次遍历后取 contentRight=110，然后 actualWidth=panel.Width ?? (contentRight+0)=110。但由于第二次布局，所以最终实际宽度要看第二次遍历。根据代码——第一次遍历时 LayoutChildren 设置子元素 LocalBounds，然后计算 contentRight=max(contentRight, child.LocalBounds.Right) = max(10+100=110, 150+200=350) = 350。然后 actualWidth = 350。然后第二次布局以 actualWidth 重算子元素——最终子元素位置应保持不变。)
- **验证点**: Panel.ActualWidth==350, Panel.ActualHeight==80, 子元素 LayoutBounds 正确

### 1.3 Panel 固定尺寸
- **用例名**: `PreLayout_AbsolutePanel_FixedSize_SubelementsInside`
- **输入**: Panel(W=400,H=300) + Rect(X=50,Y=50,W=200,H=100)
- **预期**: Panel.ActualWidth==400, Panel.ActualHeight==300
- **验证点**: Panel 固定尺寸，子元素坐标相对父容器原点

### 1.4 Panel Padding 对子元素坐标的影响
- **用例名**: `PreLayout_AbsolutePanel_Padding_OffsetsChildren`
- **输入**: Panel(Padding=16,W=200,H=200) + Rect(X=0,Y=0,W=50,H=50)
- **预期**: Rect.LayoutBounds=(16,16,50,50)
- **验证点**: X 和 Y 偏移 Padding 值

### 1.5 Panel Padding 使子元素原点偏移
- **用例名**: `PreLayout_AbsolutePanel_Padding_AutoSize`
- **输入**: Panel(Padding=16) + Rect(X=0,Y=0,W=100,H=50)
- **预期**: Panel.ActualWidth=0+50+16+16=132（contentRight=0+100=100，+16×2=132），Panel.ActualHeight=0+50+16+16=82，Rect.LayoutBounds=(16,16,100,50)
- **验证点**: 自动尺寸含 Padding，子元素偏移 Padding

### 1.6 Panel 嵌套（绝对定位内嵌绝对定位）
- **用例名**: `PreLayout_NestedAbsolutePanels_LayoutCorrect`
- **输入**: Panel(outer, W=300,H=300) + Panel(inner, X=50,Y=50,W=200,H=200) + Rect(X=10,Y=10,W=50,H=50)
- **预期**: inner.LayoutBounds=(50,50,200,200)，rect.LayoutBounds 相对 inner 内容区而非 outer
- **验证点**: 内层 Panel 的子元素坐标正确

### 1.7 对齐方式影响元素位置（无 X/Y）
- **用例名**: `PreLayout_AbsolutePanel_HorizontalAlignment_Center`
- **输入**: Panel(ParentBounds=1280x720) + Rect(HorizontalAlignment=Center,W=200,H=100)
- **预期**: Rect.LayoutBounds.X=(1280-200)/2=540
- **验证点**: 居中计算

### 1.8 右/底对齐
- **用例名**: `PreLayout_AbsolutePanel_HorizontalAlignment_Right`
- **输入**: Panel(W=800) + Rect(HorizontalAlignment=Right,W=200,H=100)
- **预期**: Rect.LayoutBounds.X=800-200=600
- **验证点**: 右对齐

### 1.9 垂直居中
- **用例名**: `PreLayout_AbsolutePanel_VerticalAlignment_Center`
- **输入**: Panel(H=600) + Rect(VerticalAlignment=Center,W=100,H=200)
- **预期**: Rect.LayoutBounds.Y=(600-200)/2=200
- **验证点**: 垂直居中

### 1.10 X/Y 优先于 Alignment
- **用例名**: `PreLayout_AbsolutePanel_ExplicitXY_PrecedesAlignment`
- **输入**: Panel(W=800,H=600) + Rect(X=10,Y=20,HorizontalAlignment=Center,VerticalAlignment=Bottom,W=200,H=100)
- **预期**: Rect.LayoutBounds=(10,20,200,100)，X/Y 覆盖对齐
- **验证点**: X/Y 值优先

### 1.11 元素超出父容器但 clipToParent=false（Page 直接子元素）
- **用例名**: `PreLayout_ChildExceedsParent_NoClipWarning_AtPage`
- **输入**: Page(1280x720) + Rect(X=-100,Y=-100,W=2000,H=2000)
- **预期**: 产生画布边界 Warning（左/上/右/下均超出），但不产生 clipToParent Warning（Page 直接子元素 clipToParent=false）
- **验证点**: Warnings 包含画布边界警告，不包含父容器裁剪警告

---

## 2. PreLayout — 水平流式布局

### 2.1 [已有] 基本水平排列 + Gap
- **用例名**: `HorizontalLayout_ChildrenPlacedSequentially`
- **输入**: Panel(Horizontal,Gap=8) + Rect(W=100,H=50) + Rect(W=200,H=50) + Rect(W=150,H=50)
- **预期**: r1.X=0, r2.X=108, r3.X=316, panel.W=466
- **验证点**: 子元素依次排列

### 2.2 无 Gap 时子元素紧贴
- **用例名**: `PreLayout_HorizontalFlow_NoGap_ChildrenTouching`
- **输入**: Panel(Horizontal) + Rect(W=100,H=50) + Rect(W=200,H=50)
- **预期**: r1.X=0, r2.X=100, panel.W=300
- **验证点**: 无 Gap 时子元素紧贴

### 2.3 [已有] Margin 影响间距（Gap vs Margin 取最大值）
- **用例名**: `HorizontalLayout_MarginAffectsSpacing`
- **输入**: Panel(Horizontal,Gap=4) + Rect(W=100,H=50,Margin.Right=20) + Rect(W=100,H=50,Margin.Left=10)
- **预期**: r1.X=0, r2.X=100+max(4,20+10)=130, panel.W=230
- **验证点**: 间距=max(Gap, prevTrailingMargin+leadingMargin)

### 2.4 [已有] Padding 偏移
- **用例名**: `HorizontalLayout_WithPadding_OffsetsContent`
- **输入**: Panel(Horizontal,Padding=16,Gap=8) + Rect(W=100,H=50)
- **预期**: r1.LayoutBounds=(16,16,100,50), panel.W=132
- **验证点**: Padding 偏移

### 2.5 [已有] 跨轴对齐（VerticalAlignment: Center/Bottom）
- **用例名**: `HorizontalLayout_CrossAxisAlignment_Respected`
- **输入**: Panel(Horizontal,H=200,Gap=8) + Rect(W=100,H=50,VerticalAlignment=Center) + Rect(W=100,H=50,VerticalAlignment=Bottom)
- **预期**: r1.Y=75, r2.Y=150
- **验证点**: 跨轴对齐

### 2.6 跨轴使用显式 Y（Y 优先于 VerticalAlignment）
- **用例名**: `PreLayout_HorizontalFlow_ExplicitY_OverridesAlignment`
- **输入**: Panel(Horizontal,H=200,Gap=8) + Rect(W=100,H=50,Y=10,VerticalAlignment=Center)
- **预期**: rect.LayoutBounds.Y=10（Y 覆盖居中）
- **验证点**: Y 优先于跨轴对齐

### 2.7 Panel 自动宽度
- **用例名**: `PreLayout_HorizontalFlow_AutoWidth`
- **输入**: Panel(Horizontal,Gap=8) + Rect(W=100) + Rect(W=150) + Rect(W=80)
- **预期**: panel.W=100+8+150+8+80=346
- **验证点**: 自动包裹

### 2.8 Panel 固定宽度 + 溢出 Warning
- **用例名**: `FlowLayout_Overflow_GeneratesWarning` [已有]
- **输入**: Panel(Horizontal,W=150,Gap=8) + Rect(W=100,H=50) + Rect(W=100,H=50)
- **预期**: Warnings.Count>0，包含溢出信息
- **验证点**: Warning 内容

### 2.9 Panel 固定高度不影响流式排列
- **用例名**: `PreLayout_HorizontalFlow_FixedHeight`
- **输入**: Panel(Horizontal,H=200,Gap=8) + Rect(W=100,H=50) + Rect(W=100,H=50)
- **预期**: panel.H=200，子元素 Y=0（默认 Top）
- **验证点**: 固定高度、Y 位置

### 2.10 [已有] 空子元素列表
- **用例名**: `FlowLayout_EmptyChildren_DoesNotCrash`
- **输入**: Panel(Horizontal,Gap=8, 无子元素)
- **预期**: panel.W=0, panel.H=0
- **验证点**: 不崩溃

### 2.11 单个子元素
- **用例名**: `PreLayout_HorizontalFlow_SingleChild`
- **输入**: Panel(Horizontal,Gap=8) + Rect(W=200,H=100)
- **预期**: r1.X=0, panel.W=200
- **验证点**: 单个元素的正确排列

### 2.12 混合类型子元素
- **用例名**: `PreLayout_HorizontalFlow_MixedElementTypes`
- **输入**: Panel(Horizontal,Gap=8) + Rect(W=100,H=50) + TextElement(W=150,H=24) + Image(W=80,H=80)
- **预期**: rect.X=0, text.X=108, image.X=108+150+8=266
- **验证点**: 不同类型元素依次排列

### 2.13 TextElement 默认尺寸 0×0 在 PreLayout 中
- **用例名**: `PreLayout_HorizontalFlow_TextElementZeroSize`
- **输入**: Panel(Horizontal) + TextElement(Text="Hello", 无 Width/Height)
- **预期**: text.ActualWidth=0, text.ActualHeight=0（因 PreLayout 无测量值）
- **验证点**: 默认尺寸

### 2.14 Image 默认尺寸 240×180
- **用例名**: `PreLayout_HorizontalFlow_ImageDefaultSize`
- **输入**: Panel(Horizontal) + Image(Source="img", 无 Width/Height)
- **预期**: img.ActualWidth=240, img.ActualHeight=180
- **验证点**: 默认尺寸

### 2.15 子元素有 Margin.Top/Bottom（水平流式中被忽略为跨轴间距）
- **用例名**: `PreLayout_HorizontalFlow_MarginTopBottom_CrossAxisNotAffected`
- **输入**: Panel(Horizontal,H=200) + Rect(W=100,H=50,Margin.Top=20) + Rect(W=100,H=50)
- **预期**: 跨轴方向不受各元素 Margin 影响。第一个子元素 leadingMargin=0（因为 i==0 时只有 leadingMargin，这里是 Top 不被考虑，因为 isHorizontal 时 leadingMargin=margin.Left）
- **验证点**: Margin 方向选择正确

### 2.16 流式布局内容宽度刚好等于 Panel 宽度（边界情况）
- **用例名**: `PreLayout_HorizontalFlow_ExactFit_NoWarning`
- **输入**: Panel(Horizontal,W=208,Gap=8) + Rect(W=100,H=50) + Rect(W=100,H=50)
- **预期**: totalFlowSize=100+8+100=208 == fixedWidth=208, 无 Warning
- **验证点**: 无 Warning（条件为 totalFlowSize > fixedWidth + 0.1）

---

## 3. PreLayout — 垂直流式布局

### 3.1 [已有] 基本垂直排列 + Gap
- **用例名**: `VerticalLayout_ChildrenPlacedSequentially`
- **输入**: Panel(Vertical,Gap=12) + Rect(W=200,H=40) + Rect(W=200,H=60)
- **预期**: r1.Y=0, r2.Y=40+12=52, panel.H=112
- **验证点**: 垂直排列

### 3.2 Margin 影响间距（垂直）
- **用例名**: `PreLayout_VerticalFlow_MarginAffectsSpacing`
- **输入**: Panel(Vertical,Gap=4) + Rect(H=40,Margin.Bottom=20) + Rect(H=60,Margin.Top=10)
- **预期**: r1.Y=0, r2.Y=40+max(4,20+10)=70, panel.H=130
- **验证点**: 垂直方向间距

### 3.3 跨轴对齐（HorizontalAlignment: Center/Right）
- **用例名**: `PreLayout_VerticalFlow_CrossAxisAlignment_Horizontal`
- **输入**: Panel(Vertical,W=300,Gap=8) + Rect(W=100,H=40,HorizontalAlignment=Center) + Rect(W=100,H=40,HorizontalAlignment=Right)
- **预期**: r1.X=(300-100)/2=100, r2.X=300-100=200
- **验证点**: 垂直流式的水平跨轴对齐

### 3.4 Panel 自动高度
- **用例名**: `PreLayout_VerticalFlow_AutoHeight`
- **输入**: Panel(Vertical,Gap=10) + Rect(H=50) + Rect(H=80) + Rect(H=60)
- **预期**: panel.H=50+10+80+10+60=210
- **验证点**: 自动包裹

### 3.5 Panel 固定高度 + 溢出 Warning
- **用例名**: `PreLayout_VerticalFlow_FixedHeightOverflow_Warning`
- **输入**: Panel(Vertical,H=100,Gap=8) + Rect(H=80) + Rect(H=80)
- **预期**: Warnings 包含 `流式布局内容高度...超出`
- **验证点**: 垂直溢出警告

### 3.6 单个子元素垂直排列
- **用例名**: `PreLayout_VerticalFlow_SingleChild`
- **输入**: Panel(Vertical) + Rect(W=200,H=100)
- **预期**: r1.Y=0, panel.H=100
- **验证点**: 单个元素

---

## 4. PreLayout — 嵌套流式布局

### 4.1 [已有] 水平内嵌水平
- **用例名**: `NestedFlowPanels_LayoutCorrectly`
- **输入**: Panel(Vertical,Gap=10) + Rect(H=40) + Panel(Horizontal,Gap=4) + Rect(W=50,H=30) + Rect(W=50,H=30)
- **预期**: inner panel.Y=50, ir1.X=0, ir2.X=54
- **验证点**: 嵌套层级

### 4.2 垂直内嵌垂直
- **用例名**: `PreLayout_NestedVerticalInVertical`
- **输入**: Panel(Vertical,Gap=10) + Rect(H=30) + Panel(Vertical,Gap=6) + Rect(H=20) + Rect(H=30)
- **预期**: innerPanel.Y=40, ir1.Y=0, ir2.Y=20+6=26
- **验证点**: 内层垂直排列

### 4.3 水平内嵌垂直
- **用例名**: `PreLayout_HorizontalContainingVertical`
- **输入**: Panel(Horizontal,Gap=10) + Panel(Vertical,Gap=8) + Rect(H=30) + Rect(H=40) + Rect(W=100,H=50)
- **预期**: 内层 Panel 作为一个整体在外层水平排列
- **验证点**: 混合方向

### 4.4 三层嵌套
- **用例名**: `PreLayout_ThreeLevelNesting`
- **输入**: Panel(Horizontal) + Panel(Vertical) + Panel(Horizontal) + 3 个 Rect
- **预期**: 最内层 Rect 坐标正确，所有 Panel 自动尺寸正确
- **验证点**: 多层嵌套

---

## 5. PreLayout — Rect 元素

### 5.1 Rect 默认尺寸 0×0
- **用例名**: `PreLayout_Rect_DefaultSize`
- **输入**: Rect(无 Width/Height)
- **预期**: rect.ActualWidth=0, rect.ActualHeight=0
- **验证点**: 默认零尺寸

### 5.2 Rect 显式尺寸
- **用例名**: `PreLayout_Rect_ExplicitSize`
- **输入**: Rect(W=200,H=100,X=50,Y=30)
- **预期**: LayoutBounds=(50,30,200,100)
- **验证点**: 尺寸与坐标

### 5.3 Rect 带对齐
- **用例名**: `PreLayout_Rect_Alignment_Center`
- **输入**: Panel(W=400,H=300) + Rect(W=100,H=50,HorizontalAlignment=Center,VerticalAlignment=Center)
- **预期**: LayoutBounds=(150,125,100,50)
- **验证点**: 居中

---

## 6. PreLayout — TextElement 元素

### 6.1 TextElement 显式尺寸
- **用例名**: `PreLayout_TextElement_ExplicitSize`
- **输入**: TextElement(W=400,H=30,X=10,Y=10)
- **预期**: LayoutBounds=(10,10,400,30)
- **验证点**: 显式尺寸

### 6.2 TextElement 默认尺寸 0×0（PreLayout）
- **用例名**: `PreLayout_TextElement_DefaultSize`
- **输入**: TextElement(Text="Hello", 无 Width/Height)
- **预期**: ActualWidth=0, ActualHeight=0
- **验证点**: PreLayout 默认零尺寸

---

## 7. FinalLayout

### 7.1 [已有] 使用测量值替代声明值
- **用例名**: `FinalLayout_UsesMeasuredSizes`
- **输入**: Panel(Horizontal,Gap=8) + TextElement(t1) + TextElement(t2)；测量值: t1=(120,24), t2=(100,24)
- **预期**: t1.ActualWidth=120, t2.ActualWidth=100, t2.X=120+8=128
- **验证点**: 测量值生效

### 7.2 TextElement 的 ActualLineCount 回填
- **用例名**: `FinalLayout_TextElement_ActualLineCountBackfill`
- **输入**: TextElement(W=200,Text="...",Height=50)；测量值: MeasuredHeight=48, ActualLineCount=3
- **预期**: text.ActualLineCount==3
- **验证点**: 行数回填

### 7.3 文本溢出容器高度 Warning
- **用例名**: `FinalLayout_TextHeightOverflow_Warning`
- **输入**: TextElement(W=400,Height=30,Text="...multiple lines...")；测量值: MeasuredHeight=80, ActualLineCount=5
- **预期**: Warnings 包含 `超出容器高度`
- **验证点**: Warning 触发

### 7.4 文本刚好不溢出（边界情况）
- **用例名**: `FinalLayout_TextHeightExactlyFit_NoWarning`
- **输入**: TextElement(W=400,Height=48,Text="...")；测量值: MeasuredHeight=48, ActualLineCount=3
- **预期**: 无文本溢出 Warning
- **验证点**: 边界不触发

### 7.5 Image 使用测量尺寸
- **用例名**: `FinalLayout_Image_MeasuredSize`
- **输入**: Image(Source="img", 无声明 Width/Height)；测量值: (800,600)
- **预期**: image.ActualWidth=800, image.ActualHeight=600
- **验证点**: 图片测量尺寸

### 7.6 声明 Width 优先于测量值
- **用例名**: `FinalLayout_DeclaredWidth_PrecedesMeasured`
- **输入**: TextElement(W=500,Text="Hello")；测量值: (120,24)
- **预期**: text.ActualWidth=500（声明值优先）
- **验证点**: `Width ?? MeasuredWidth` 逻辑

### 7.7 Rect 没有测量值（不受影响）
- **用例名**: `FinalLayout_Rect_NoMeasurement_Unchanged`
- **输入**: Rect(W=100,H=50) + 测量值字典中无此 Rect 的 Id
- **预期**: rect.ActualWidth=100, rect.ActualHeight=50
- **验证点**: 无测量值的 Rect 不受影响

---

## 8. 边界校验（ValidateBounds）

### 8.1 元素右边界超出画布宽度
- **用例名**: `ValidateBounds_ElementRightExceedsCanvas_Warning`
- **输入**: Page(1280x720) + Rect(X=1200,W=200,H=50)
- **预期**: Warnings 包含 `右边界 X=1400 超出画布宽度 1280`
- **验证点**: Warning 内容

### 8.2 元素下边界超出画布高度
- **用例名**: `ValidateBounds_ElementBottomExceedsCanvas_Warning`
- **输入**: Page(1280x720) + Rect(Y=700,H=50,W=100)
- **预期**: Warnings 包含 `下边界 Y=750 超出画布高度 720`
- **验证点**: Warning 内容

### 8.3 元素左边界 < 0
- **用例名**: `ValidateBounds_ElementLeftNegative_Warning`
- **输入**: Page(1280x720) + Rect(X=-50,W=100,H=50)
- **预期**: Warnings 包含 `左边界 X=-50 超出画布左侧 0`
- **验证点**: Warning 内容

### 8.4 元素上边界 < 0
- **用例名**: `ValidateBounds_ElementTopNegative_Warning`
- **输入**: Page(1280x720) + Rect(Y=-20,W=100,H=50)
- **预期**: Warnings 包含 `上边界 Y=-20 超出画布顶部 0`
- **验证点**: Warning 内容

### 8.5 元素在画布内 → 无 Warning
- **用例名**: `ValidateBounds_ElementInsideCanvas_NoWarning`
- **输入**: Page(1280x720) + Rect(X=100,Y=100,W=400,H=300)
- **预期**: Warnings 无画布边界警告
- **验证点**: 无 Warning

### 8.6 元素刚好在边界上（Right=CanvasWidth）
- **用例名**: `ValidateBounds_ElementExactlyAtEdge_NoWarning`
- **输入**: Page(1280x720) + Rect(X=1180,W=100,H=50) → Right=1280
- **预期**: 无 Warning（Right==CanvasWidth，条件为 > 非 >=）
- **验证点**: 边界值不触发

### 8.7 元素超出父容器（clipToParent=true）
- **用例名**: `ValidateBounds_ChildExceedsParentContainer_Warning`
- **输入**: Panel(X=0,Y=0,W=200,H=200) + Rect(X=150,Y=150,W=100,H=100)
- **预期**: Warnings 包含 `超出父容器`
- **验证点**: clipToParent 警告

### 8.8 元素在父容器内 → 无裁剪警告
- **用例名**: `ValidateBounds_ChildInsideParent_NoClipWarning`
- **输入**: Panel(W=300,H=300) + Rect(X=50,Y=50,W=200,H=200)
- **预期**: 无父容器裁剪警告
- **验证点**: 无 Warning

---

## 9. ResolveOrigin 逻辑

### 9.1 显式 X → parentOrigin + offset
- **用例名**: `ResolveOrigin_Horizontal_ExplicitX`
- **输入**: parentOrigin=0, parentSize=1280, elementSize=100, X=50, alignment=Left
- **预期**: 50
- **验证点**: `parentOrigin + X`

### 9.2 HorizontalAlignment.Center
- **用例名**: `ResolveOrigin_Horizontal_AlignmentCenter`
- **输入**: parentSize=1280, elementSize=200
- **预期**: (1280-200)/2=540
- **验证点**: 居中

### 9.3 HorizontalAlignment.Right
- **用例名**: `ResolveOrigin_Horizontal_AlignmentRight`
- **输入**: parentSize=1280, elementSize=200
- **预期**: 1280-200=1080
- **验证点**: 右对齐

### 9.4 元素比父容器大时 max(0, ...) 保护
- **用例名**: `ResolveOrigin_Horizontal_ElementLargerThanParent`
- **输入**: parentSize=200, elementSize=400, alignment=Center
- **预期**: 0（因 Math.Max(0, (200-400)/2)=0）
- **验证点**: 不产生负坐标

### 9.5 VerticalAlignment.Center
- **用例名**: `ResolveOrigin_Vertical_AlignmentCenter`
- **输入**: parentSize=600, elementSize=200
- **预期**: (600-200)/2=200
- **验证点**: 垂直居中

### 9.6 VerticalAlignment.Bottom
- **用例名**: `ResolveOrigin_Vertical_AlignmentBottom`
- **输入**: parentSize=600, elementSize=200
- **预期**: 600-200=400
- **验证点**: 底部对齐

---

## 10. GetChildSize 逻辑

### 10.1 有测量值时使用测量值
- **用例名**: `GetChildSize_WithMeasurement_ReturnsMeasured`
- **输入**: TextElement(W nullable), measurements={"t1": (200,40)}
- **预期**: (200,40)
- **验证点**: 测量值返回

### 10.2 声明 Width 优先于测量值
- **用例名**: `GetChildSize_DeclaredWidth_WithMeasurement`
- **输入**: TextElement(W=500), measurements={"t1": (200,40)}
- **预期**: (500,40) —— Width 使用声明值，Height 使用测量值（当声明 Height 为 null 时）
- **验证点**: child.Width ?? measured

### 10.3 无测量值时 TextElement 默认 (0,0)
- **用例名**: `GetChildSize_TextElement_Default`
- **输入**: TextElement(无 Width/Height), 无测量值
- **预期**: (0,0)
- **验证点**: TextElement 默认

### 10.4 无测量值时 Image 默认 (240,180)
- **用例名**: `GetChildSize_Image_Default`
- **输入**: Image(无 Width/Height), 无测量值
- **预期**: (240,180)
- **验证点**: Image 默认

### 10.5 无测量值时 Rect 默认 (0,0)
- **用例名**: `GetChildSize_Rect_Default`
- **输入**: Rect(无 Width/Height), 无测量值
- **预期**: (0,0)
- **验证点**: Rect 默认

---

## 11. 特殊场景

### 11.1 自定义画布尺寸
- **用例名**: `PreLayout_CustomCanvasSize`
- **输入**: Context(CanvasWidth=1920, CanvasHeight=1080) + Page + Rect(X=1800,Y=1000,W=200,H=200)
- **预期**: page.LayoutBounds=(0,0,1920,1080)，rect 不超出（1800+200=2000>1920→右边界警告）
- **验证点**: 自定义画布下的布局和边界校验

### 11.2 零尺寸元素
- **用例名**: `PreLayout_ZeroSizeElement_NoCrash`
- **输入**: Rect(W=0,H=0)
- **预期**: ActualWidth=0, ActualHeight=0，无崩溃
- **验证点**: 零尺寸不崩溃

### 11.3 Panel 只有 Padding 无子元素
- **用例名**: `PreLayout_PanelPaddingOnly_NoChildren`
- **输入**: Panel(Padding=24, 无子元素)
- **预期**: panel.ActualWidth=0（contentRight=0, actualWidth=0+24×2=48? 不，因为 contentRight 初始为 0，没有子元素所以不更新。actualWidth=0(Width)??(0+48)=48）。需要验证具体逻辑。
- **验证点**: 无子元素但有 Padding 时的自动尺寸

### 11.4 流式布局中子元素有显式排列轴方向的坐标
- **用例名**: `PreLayout_FlowChildExplicitAxisPosition`
- **输入**: Panel(Horizontal,Gap=8) + Rect(X=50,W=100,H=50) + Rect(W=100,H=50)
- **预期**: 水平排列时第一个 Rect 的 X=50 被忽略？不——根据代码，流式排列时 flowPosition 只从 contentOriginX 开始 + 第一个子元素的 leadingMargin，而不使用子元素的 X。所以 r1.X=0（排列起点），r2.X=100+8=108。子元素的 X 在流式布局中不影响排列轴，仅影响 LocalBounds。
- **验证点**: 流式布局中排列轴方向的 X/Y 被忽略，仅影响 LocalBounds 的 X 值

---

## 12. 幂等性/一致性

### 12.1 重复调用 PreLayout 结果一致
- **用例名**: `PreLayout_Twice_Idempotent`
- **输入**: Panel(Horizontal,Gap=8) + Rect(W=100,H=50) + Rect(W=100,H=50)
- **步骤**: 两次调用 PreLayout
- **预期**: 两次结果完全一致
- **验证点**: 幂等性

### 12.2 PreLayout 后调用 FinalLayout 覆盖正确
- **用例名**: `PreLayoutThenFinalLayout_CorrectOverride`
- **输入**: TextElement(W=500,Text="Hello")，测量值 (120,24)
- **步骤**: 先 PreLayout (W=500,H=0)，再 FinalLayout (W=500,H=24)
- **预期**: FinalLayout 使用测量值覆盖 ActualHeight
- **验证点**: 尺寸正确更新