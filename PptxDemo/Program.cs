using System;
using System.Diagnostics;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using dotnetCampus.OpenXmlUnitConverter;
using GroupShape = DocumentFormat.OpenXml.Presentation.GroupShape;
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
            /*
             * <p:timing>
                <p:tnLst>
                  <p:par>
                    <p:cTn id="1" dur="indefinite" restart="never" nodeType="tmRoot">
             */
            // 第一级里面默认只有一项
            var commonTimeNode = timing?.TimeNodeList?.ParallelTimeNode?.CommonTimeNode;

            if (commonTimeNode?.NodeType?.Value == TimeNodeValues.TmingRoot)
            {
                // 这是符合约定
                // nodeType="tmRoot"
            }

            if (commonTimeNode?.ChildTimeNodeList == null) return;
            // <p:childTnLst>
            //   <p:seq concurrent="1" nextAc="seek">
            // 理论上只有一项，而且一定是 SequenceTimeNode 类型
            var sequenceTimeNode = commonTimeNode.ChildTimeNodeList.GetFirstChild<SequenceTimeNode>();

            // <p:cTn id="2" dur="indefinite" nodeType="mainSeq">
            var mainSequenceTimeNode = sequenceTimeNode.CommonTimeNode;
            if (mainSequenceTimeNode?.NodeType?.Value == TimeNodeValues.MainSequence)
            {
                // <p:childTnLst>
                // [TimeLine 对象 (PowerPoint) | Microsoft Docs](https://docs.microsoft.com/zh-cn/office/vba/api/PowerPoint.TimeLine )
                //  MainSequence 主动画序列
                ChildTimeNodeList mainChildTimeNodeList = mainSequenceTimeNode.ChildTimeNodeList!;
                // <p:par>
                var mainParallelTimeNode = mainChildTimeNodeList!.GetFirstChild<ParallelTimeNode>();
                // <p:cTn id="3" fill="hold">
                var subCommonTimeNode = mainParallelTimeNode!.CommonTimeNode;
                // <p:childTnLst>
                var subChildTimeNodeList = subCommonTimeNode!.ChildTimeNodeList;
                foreach (var openXmlElement in subChildTimeNodeList!)
                {
                    // 按照顺序获取
                    // <p:par>
                    // <!-- 进入动画-->
                    // </p:par>
                    // <p:par>
                    // <!-- 强调动画-->
                    // </p:par>
                    // <p:par>
                    // <!-- 退出动画-->
                    // </p:par>
                    if (openXmlElement is ParallelTimeNode parallelTimeNode)
                    {
                        var timeNode = parallelTimeNode!.CommonTimeNode!.ChildTimeNodeList!.GetFirstChild<ParallelTimeNode>()!.CommonTimeNode;
                        switch (timeNode!.PresetClass!.Value)
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
                else if(openXmlElement is GroupShape groupShape)
                {
                    ReadGroupShape(groupShape);
                }
            }
        }

        private static void ReadGroupShape(GroupShape groupShape)
        {
            foreach (var openXmlElement in groupShape.ChildElements)
            {
                if (openXmlElement is Shape shape)
                {
                    ReadShape(shape);
                }
                else if (openXmlElement is GroupShape group)
                {
                    ReadGroupShape(group);
                }
            }
        }

        private static void ReadShape(Shape shape)
        {
            ReadFill(shape);

            ReadLineWidth(shape);
        }

        private static void ReadFill(Shape shape)
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
                    while (groupShape!=null)
                    {
                        groupShape = groupShape.Parent as GroupShape;
                        solidFill = groupShape?.GroupShapeProperties?.GetFirstChild<SolidFill>();

                        if (solidFill != null)
                        {
                            break;
                        }
                    }
                }

                Debug.Assert(solidFill?.RgbColorModelHex?.Val != null, "solidFill.RgbColorModelHex.Val != null");
                Console.WriteLine(solidFill.RgbColorModelHex.Val.Value);
            }
            else
            {
                var solidFill = shapeProperties.GetFirstChild<SolidFill>();

                Debug.Assert(solidFill?.RgbColorModelHex?.Val != null, "solidFill.RgbColorModelHex.Val != null");
                Console.WriteLine(solidFill.RgbColorModelHex.Val.Value);
            }
        }



        private static void ReadLineWidth(Shape shape)
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