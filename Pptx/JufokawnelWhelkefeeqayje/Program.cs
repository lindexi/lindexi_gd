// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using JufokawnelWhelkefeeqayje.Framework.CommonGenerator;
using JufokawnelWhelkefeeqayje.Framework.Context;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P14 = DocumentFormat.OpenXml.Office2010.PowerPoint;

var file = new FileInfo("1.pptx");

using var document =
    CommonPresentationDocumentGenerator.CreateEmptyDocument(file, new DocumentInfo("lindexi"));

var presentationPart = document.PresentationPart;
var firstSlide = presentationPart!.SlideParts.First().Slide;
var shapeTree = firstSlide.CommonSlideData!.ShapeTree!;

var generateGraphicFrame = GenerateGraphicFrame();
shapeTree.AppendChild(generateGraphicFrame);

Console.WriteLine("Hello, World!");

GraphicFrame GenerateGraphicFrame()
{
    GraphicFrame graphicFrame1 = new GraphicFrame();

    NonVisualGraphicFrameProperties nonVisualGraphicFrameProperties1 = new NonVisualGraphicFrameProperties();

    NonVisualDrawingProperties nonVisualDrawingProperties1 = new NonVisualDrawingProperties() { Id = (UInt32Value) 5U, Name = "表格 1" };

    A.NonVisualDrawingPropertiesExtensionList nonVisualDrawingPropertiesExtensionList1 = new A.NonVisualDrawingPropertiesExtensionList();

    A.NonVisualDrawingPropertiesExtension nonVisualDrawingPropertiesExtension1 = new A.NonVisualDrawingPropertiesExtension() { Uri = "{FF2B5EF4-FFF2-40B4-BE49-F238E27FC236}" };

    OpenXmlUnknownElement openXmlUnknownElement1 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement("<a16:creationId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" id=\"{CD17561D-9B93-D5E7-0F0D-87F8D105C682}\" />");

    nonVisualDrawingPropertiesExtension1.Append(openXmlUnknownElement1);

    nonVisualDrawingPropertiesExtensionList1.Append(nonVisualDrawingPropertiesExtension1);

    nonVisualDrawingProperties1.Append(nonVisualDrawingPropertiesExtensionList1);

    NonVisualGraphicFrameDrawingProperties nonVisualGraphicFrameDrawingProperties1 = new NonVisualGraphicFrameDrawingProperties();
    A.GraphicFrameLocks graphicFrameLocks1 = new A.GraphicFrameLocks() { NoGrouping = true };

    nonVisualGraphicFrameDrawingProperties1.Append(graphicFrameLocks1);

    ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties1 = new ApplicationNonVisualDrawingProperties();

    ApplicationNonVisualDrawingPropertiesExtensionList applicationNonVisualDrawingPropertiesExtensionList1 = new ApplicationNonVisualDrawingPropertiesExtensionList();

    ApplicationNonVisualDrawingPropertiesExtension applicationNonVisualDrawingPropertiesExtension1 = new ApplicationNonVisualDrawingPropertiesExtension() { Uri = "{D42A27DB-BD31-4B8C-83A1-F6EECF244321}" };

    P14.ModificationId modificationId1 = new P14.ModificationId() { Val = (UInt32Value) 2563849206U };
    modificationId1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

    applicationNonVisualDrawingPropertiesExtension1.Append(modificationId1);

    applicationNonVisualDrawingPropertiesExtensionList1.Append(applicationNonVisualDrawingPropertiesExtension1);

    applicationNonVisualDrawingProperties1.Append(applicationNonVisualDrawingPropertiesExtensionList1);

    nonVisualGraphicFrameProperties1.Append(nonVisualDrawingProperties1);
    nonVisualGraphicFrameProperties1.Append(nonVisualGraphicFrameDrawingProperties1);
    nonVisualGraphicFrameProperties1.Append(applicationNonVisualDrawingProperties1);

    Transform transform1 = new Transform();
    A.Offset offset1 = new A.Offset() { X = 2032000L, Y = 719666L };
    A.Extents extents1 = new A.Extents() { Cx = 7985458L, Cy = 731520L };

    transform1.Append(offset1);
    transform1.Append(extents1);

    A.Graphic graphic1 = new A.Graphic();

    A.GraphicData graphicData1 = new A.GraphicData() { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" };

    A.Table table1 = new A.Table();

    A.TableProperties tableProperties1 = new A.TableProperties() { FirstRow = true, BandRow = true };
    A.TableStyleId tableStyleId1 = new A.TableStyleId();
    tableStyleId1.Text = "{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}";

    tableProperties1.Append(tableStyleId1);

    A.TableGrid tableGrid1 = new A.TableGrid();

    A.GridColumn gridColumn1 = new A.GridColumn() { Width = 3992729L };

    A.ExtensionList extensionList1 = new A.ExtensionList();

    A.Extension extension1 = new A.Extension() { Uri = "{9D8B030D-6E8A-4147-A177-3AD203B41FA5}" };

    OpenXmlUnknownElement openXmlUnknownElement2 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement("<a16:colId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" val=\"3797773054\" />");

    extension1.Append(openXmlUnknownElement2);

    extensionList1.Append(extension1);

    gridColumn1.Append(extensionList1);

    A.GridColumn gridColumn2 = new A.GridColumn() { Width = 3992729L };

    A.ExtensionList extensionList2 = new A.ExtensionList();

    A.Extension extension2 = new A.Extension() { Uri = "{9D8B030D-6E8A-4147-A177-3AD203B41FA5}" };

    OpenXmlUnknownElement openXmlUnknownElement3 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement("<a16:colId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" val=\"3065667247\" />");

    extension2.Append(openXmlUnknownElement3);

    extensionList2.Append(extension2);

    gridColumn2.Append(extensionList2);

    tableGrid1.Append(gridColumn1);
    tableGrid1.Append(gridColumn2);

    A.TableRow tableRow1 = new A.TableRow() { Height = 295275L };

    A.TableCell tableCell1 = new A.TableCell();

    // 加上这句话将导致 WPS 表格丢失文字
    tableCell1.VerticalMerge = false;

    A.TextBody textBody1 = new A.TextBody();
    A.BodyProperties bodyProperties1 = new A.BodyProperties();
    A.ListStyle listStyle1 = new A.ListStyle();

    A.Paragraph paragraph1 = new A.Paragraph();

    A.Run run1 = new A.Run();
    A.RunProperties runProperties1 = new A.RunProperties() { Language = "en-US", AlternativeLanguage = "zh-CN", Dirty = false };
    A.Text text1 = new A.Text();
    text1.Text = "1";

    run1.Append(runProperties1);
    run1.Append(text1);
    A.EndParagraphRunProperties endParagraphRunProperties1 = new A.EndParagraphRunProperties() { Language = "zh-CN", AlternativeLanguage = "en-US", Dirty = false };

    paragraph1.Append(run1);
    paragraph1.Append(endParagraphRunProperties1);

    textBody1.Append(bodyProperties1);
    textBody1.Append(listStyle1);
    textBody1.Append(paragraph1);
    A.TableCellProperties tableCellProperties1 = new A.TableCellProperties();

    tableCell1.Append(textBody1);
    tableCell1.Append(tableCellProperties1);

    A.TableCell tableCell2 = new A.TableCell();

    // 加上这句话将导致 WPS 表格丢失文字
    tableCell2.HorizontalMerge = false;

    A.TextBody textBody2 = new A.TextBody();
    A.BodyProperties bodyProperties2 = new A.BodyProperties();
    A.ListStyle listStyle2 = new A.ListStyle();

    A.Paragraph paragraph2 = new A.Paragraph();

    A.Run run2 = new A.Run();
    A.RunProperties runProperties2 = new A.RunProperties() { Language = "en-US", AlternativeLanguage = "zh-CN", Dirty = false };
    A.Text text2 = new A.Text();
    text2.Text = "2";

    run2.Append(runProperties2);
    run2.Append(text2);
    A.EndParagraphRunProperties endParagraphRunProperties2 = new A.EndParagraphRunProperties() { Language = "zh-CN", AlternativeLanguage = "en-US", Dirty = false };

    paragraph2.Append(run2);
    paragraph2.Append(endParagraphRunProperties2);

    textBody2.Append(bodyProperties2);
    textBody2.Append(listStyle2);
    textBody2.Append(paragraph2);
    A.TableCellProperties tableCellProperties2 = new A.TableCellProperties();

    tableCell2.Append(textBody2);
    tableCell2.Append(tableCellProperties2);

    A.ExtensionList extensionList3 = new A.ExtensionList();

    A.Extension extension3 = new A.Extension() { Uri = "{0D108BD9-81ED-4DB2-BD59-A6C34878D82A}" };

    OpenXmlUnknownElement openXmlUnknownElement4 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement("<a16:rowId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" val=\"3965977653\" />");

    extension3.Append(openXmlUnknownElement4);

    extensionList3.Append(extension3);

    tableRow1.Append(tableCell1);
    tableRow1.Append(tableCell2);
    tableRow1.Append(extensionList3);

    A.TableRow tableRow2 = new A.TableRow() { Height = 295275L };

    A.TableCell tableCell3 = new A.TableCell();

    A.TextBody textBody3 = new A.TextBody();
    A.BodyProperties bodyProperties3 = new A.BodyProperties();
    A.ListStyle listStyle3 = new A.ListStyle();

    A.Paragraph paragraph3 = new A.Paragraph();
    A.EndParagraphRunProperties endParagraphRunProperties3 = new A.EndParagraphRunProperties() { Language = "zh-CN", AlternativeLanguage = "en-US", Dirty = false };

    paragraph3.Append(endParagraphRunProperties3);

    textBody3.Append(bodyProperties3);
    textBody3.Append(listStyle3);
    textBody3.Append(paragraph3);
    A.TableCellProperties tableCellProperties3 = new A.TableCellProperties();

    tableCell3.Append(textBody3);
    tableCell3.Append(tableCellProperties3);

    A.TableCell tableCell4 = new A.TableCell();

    A.TextBody textBody4 = new A.TextBody();
    A.BodyProperties bodyProperties4 = new A.BodyProperties();
    A.ListStyle listStyle4 = new A.ListStyle();

    A.Paragraph paragraph4 = new A.Paragraph();
    A.EndParagraphRunProperties endParagraphRunProperties4 = new A.EndParagraphRunProperties() { Language = "zh-CN", AlternativeLanguage = "en-US", Dirty = false };

    paragraph4.Append(endParagraphRunProperties4);

    textBody4.Append(bodyProperties4);
    textBody4.Append(listStyle4);
    textBody4.Append(paragraph4);
    A.TableCellProperties tableCellProperties4 = new A.TableCellProperties();

    tableCell4.Append(textBody4);
    tableCell4.Append(tableCellProperties4);

    A.ExtensionList extensionList4 = new A.ExtensionList();

    A.Extension extension4 = new A.Extension() { Uri = "{0D108BD9-81ED-4DB2-BD59-A6C34878D82A}" };

    OpenXmlUnknownElement openXmlUnknownElement5 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement("<a16:rowId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" val=\"1863066117\" />");

    extension4.Append(openXmlUnknownElement5);

    extensionList4.Append(extension4);

    tableRow2.Append(tableCell3);
    tableRow2.Append(tableCell4);
    tableRow2.Append(extensionList4);

    table1.Append(tableProperties1);
    table1.Append(tableGrid1);
    table1.Append(tableRow1);
    table1.Append(tableRow2);

    graphicData1.Append(table1);

    graphic1.Append(graphicData1);

    graphicFrame1.Append(nonVisualGraphicFrameProperties1);
    graphicFrame1.Append(transform1);
    graphicFrame1.Append(graphic1);
    return graphicFrame1;
}

