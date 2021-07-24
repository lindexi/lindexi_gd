using System;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using dotnetCampus.OpenXmlUnitConverter;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace PptxDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            using var presentationDocument =
                DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", false);
            var presentationPart = presentationDocument.PresentationPart;
            var slidePart = presentationPart!.SlideParts.First();
            var slide = slidePart.Slide;

            GetShape(slide);

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

            var mainSequenceTimeNode = sequenceTimeNode.CommonTimeNode;
            if (mainSequenceTimeNode?.NodeType?.Value == TimeNodeValues.MainSequence)
            {
                // [TimeLine 对象 (PowerPoint) | Microsoft Docs](https://docs.microsoft.com/zh-cn/office/vba/api/PowerPoint.TimeLine )
                //  MainSequence 主动画序列
                var mainParallelTimeNode = mainSequenceTimeNode.ChildTimeNodeList;

                foreach (var openXmlElement in mainParallelTimeNode)
                {
                    // 并行关系的
                    if (openXmlElement is ParallelTimeNode parallelTimeNode)
                    {
                        var timeNode = parallelTimeNode.CommonTimeNode.ChildTimeNodeList
                            .GetFirstChild<ParallelTimeNode>().CommonTimeNode.ChildTimeNodeList
                            .GetFirstChild<ParallelTimeNode>().CommonTimeNode;

                        switch (timeNode.PresetClass.Value)
                        {
                            case TimeNodePresetClassValues.Entrance:
                                // 进入动画
                                break;
                            case TimeNodePresetClassValues.Exit:
                                // 退出动画
                                break;
                            case TimeNodePresetClassValues.Emphasis:
                                // 强调动画
                                break;
                            case TimeNodePresetClassValues.Path:
                                // 路由动画
                                break;
                            case TimeNodePresetClassValues.Verb:
                                break;
                            case TimeNodePresetClassValues.MediaCall:
                                // 播放动画
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            // 文档规定，必须存在一个AttributeNameList列表，一定存在AttributeName元素，如果有多个只取第一个元素。
            // 见"[MS-OI 29500].PDF 第2.1.1137章节（g选项）"
        }

        private static void GetShape(Slide slide)
        {
            foreach (var openXmlElement in slide.CommonSlideData.ShapeTree)
            {
                if (openXmlElement is Shape shape)
                {
                    ReadShape(shape);
                }
            }
        }

        private static void ReadShape(Shape shape)
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
    }
}