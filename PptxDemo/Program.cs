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
}