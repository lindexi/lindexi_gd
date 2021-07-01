using System;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;

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
                var mainParallelTimeNode = mainSequenceTimeNode.ChildTimeNodeList
                    .GetFirstChild<ParallelTimeNode>().CommonTimeNode.ChildTimeNodeList
                    .GetFirstChild<ParallelTimeNode>().CommonTimeNode.ChildTimeNodeList
                    .GetFirstChild<ParallelTimeNode>();

                switch (mainParallelTimeNode.CommonTimeNode.PresetClass.Value)
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

            // 文档规定，必须存在一个AttributeNameList列表，一定存在AttributeName元素，如果有多个只取第一个元素。
            // 见"[MS-OI 29500].PDF 第2.1.1137章节（g选项）"
        }
    }
}
