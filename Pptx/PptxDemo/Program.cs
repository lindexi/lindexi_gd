using System.Linq;
using DocumentFormat.OpenXml.Presentation;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualShapeProperties;

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", false);
var presentationPart = presentationDocument.PresentationPart;
var slidePart = presentationPart!.SlideParts.First();
var slide = slidePart.Slide;
var timing = slide.Timing;
// 第一级里面默认只有一项
var commonTimeNode = timing?.TimeNodeList?.ParallelTimeNode?.CommonTimeNode;
if (commonTimeNode?.NodeType?.Value == TimeNodeValues.TmingRoot)
{
    // 这是符合约定
    // nodeType="tmRoot"
}

if (commonTimeNode?.ChildTimeNodeList == null) return;
// 理论上只有一项，而且一定是 SequenceTimeNode 类型
var sequenceTimeNode = commonTimeNode.ChildTimeNodeList.GetFirstChild<SequenceTimeNode>();
var interactiveSequenceTimeNode = sequenceTimeNode.CommonTimeNode;
if (interactiveSequenceTimeNode?.NodeType?.Value == TimeNodeValues.InteractiveSequence)
{
    // [TimeLine 对象 (PowerPoint) | Microsoft Docs](https://docs.microsoft.com/zh-cn/office/vba/api/PowerPoint.TimeLine )
    // 触发动画序列

    // 获取触发动画的元素
    var condition = interactiveSequenceTimeNode.StartConditionList.GetFirstChild<Condition>();
    if (condition.Event.Value == TriggerEventValues.OnClick)
    {
        // 点击触发动画，还有其他的方式
    }

    var targetElement = condition.TargetElement;
    var shapeId = targetElement.ShapeTarget.ShapeId.Value;
    var shape = slide.CommonSlideData.ShapeTree.FirstOrDefault(t =>
        t.GetFirstChild<NonVisualShapeProperties>()?.GetFirstChild<NonVisualDrawingProperties>()?.Id?.Value.ToString() == shapeId);
    // 由 shape 点击触发的动画

    foreach (var openXmlElement in interactiveSequenceTimeNode.ChildTimeNodeList)
    {
        // 并行关系的
        if (openXmlElement is ParallelTimeNode parallelTimeNode)
        {
            var timeNode = parallelTimeNode.CommonTimeNode.ChildTimeNodeList
                .GetFirstChild<ParallelTimeNode>().CommonTimeNode.ChildTimeNodeList
                .GetFirstChild<ParallelTimeNode>().CommonTimeNode;

            if (timeNode.NodeType.Value == TimeNodeValues.ClickEffect)
            {
                // 点击触发
            }

            // 其他逻辑和主序列相同
           
        }
    }
<<<<<<< HEAD
}

static void ReadShape(Shape shape)
{
    ReadFill(shape);

    ReadLineWidth(shape);
}

static void ReadFill(Shape shape)
{
    // 更多读取画刷颜色请看 [dotnet OpenXML 获取颜色方法](https://blog.lindexi.com/post/Office-%E4%BD%BF%E7%94%A8-OpenXML-SDK-%E8%A7%A3%E6%9E%90%E6%96%87%E6%A1%A3%E5%8D%9A%E5%AE%A2%E7%9B%AE%E5%BD%95.html )

    var shapeProperties = shape.ShapeProperties;
    if (shapeProperties == null)
    {
        return;
    }

    var groupFill = shapeProperties.GetFirstChild<GroupFill>();
    if (groupFill != null)
    {
        // 如果是组合的颜色画刷，那需要去获取组合的
        var groupShape = shape.Parent as GroupShape;
        var solidFill = groupShape?.GroupShapeProperties?.GetFirstChild<SolidFill>();

        if (solidFill is null)
        {
            // 继续获取组合的组合
            while (groupShape != null)
            {
                groupShape = groupShape.Parent as GroupShape;
                solidFill = groupShape?.GroupShapeProperties?.GetFirstChild<SolidFill>();

                if (solidFill != null)
                {
                    break;
                }
            }
        }

        if (solidFill is null)
        {
            Console.WriteLine($"没有颜色");
        }
        else
        {
            Debug.Assert(solidFill.RgbColorModelHex?.Val != null, "solidFill.RgbColorModelHex.Val != null");
            Console.WriteLine(solidFill.RgbColorModelHex.Val.Value);
        }
    }
    else
    {
        var solidFill = shapeProperties.GetFirstChild<SolidFill>();

        Debug.Assert(solidFill?.RgbColorModelHex?.Val != null, "solidFill.RgbColorModelHex.Val != null");
        Console.WriteLine(solidFill.RgbColorModelHex.Val.Value);
    }
}

static void ReadLineWidth(Shape shape)
{
    // 读取线条宽度的方法
    var outline = shape.ShapeProperties?.GetFirstChild<Outline>();
    if (outline != null)
    {
        var lineWidth = outline.Width;
        var emu = new Emu(lineWidth);
        var pixel = emu.ToPixel();
        Console.WriteLine($"线条宽度 {pixel.Value}");
    }
    else
    {
        // 这形状没有定义轮廓
    }
}
=======
}
>>>>>>> e48a633377bb933ad09e3782272b0a01ffd42ab5
