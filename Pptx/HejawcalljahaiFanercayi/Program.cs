using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using BlipFill = DocumentFormat.OpenXml.Drawing.Pictures.BlipFill;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties;
using NonVisualGraphicFrameDrawingProperties =
    DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties;
using NonVisualPictureDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties;
using NonVisualPictureProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using ParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using Path = System.IO.Path;
using Picture = DocumentFormat.OpenXml.Drawing.Pictures.Picture;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using ShapeProperties = DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using TableCellProperties = DocumentFormat.OpenXml.Wordprocessing.TableCellProperties;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

var wordFile = Path.Join(AppContext.BaseDirectory, "Test.docx");

using (var wordprocessingDocument = WordprocessingDocument.Open(wordFile, true))
{
    var document = wordprocessingDocument.MainDocumentPart!.Document;
    TableCell? toReplaceTableCell = null;
    foreach (var tableCell in document.Descendants<TableCell>())
    {
        foreach (var text in tableCell.Descendants<Text>())
        {
            if (text.Text == "<lindexi>")
            {
                toReplaceTableCell = tableCell;
                break;
            }
        }

        if (toReplaceTableCell != null) break;
    }

    var tableRow = toReplaceTableCell?.Parent;
    if (tableRow is not null)
    {
        using var imageFile = File.OpenRead(Path.Join(AppContext.BaseDirectory, "Image.png"));

        var imagePart = wordprocessingDocument.MainDocumentPart.AddImagePart(partType: new PartTypeInfo("image/png", ".png"));
        imagePart.FeedData(imageFile);

        var idOfPart = wordprocessingDocument.MainDocumentPart.GetIdOfPart(imagePart);

        var newTableCell = CreateTableCellWithTwoImage(idOfPart);
        tableRow.ReplaceChild(newTableCell, toReplaceTableCell);
    }

}

Console.WriteLine("Finish");

TableCell CreateTableCellWithTwoImage(string idOfPart)
{
    var tableCell = new TableCell();

    var tableCellProperties = new TableCellProperties();
    var tableCellWidth = new TableCellWidth { Width = "4673", Type = TableWidthUnitValues.Dxa };

    tableCellProperties.Append(tableCellWidth);

    var paragraph = new Paragraph
    {
        RsidParagraphAddition = "006C0B99", RsidParagraphProperties = "006C0B99", RsidRunAdditionDefault = "006C0B99",
        ParagraphId = "0DC7B369", TextId = "439F677E"
    };

    var paragraphProperties1 = new ParagraphProperties();
    var justification1 = new Justification { Val = JustificationValues.Center };

    var paragraphMarkRunProperties1 = new ParagraphMarkRunProperties();
    var runFonts1 = new RunFonts { Hint = FontTypeHintValues.EastAsia };

    paragraphMarkRunProperties1.Append(runFonts1);

    paragraphProperties1.Append(justification1);
    paragraphProperties1.Append(paragraphMarkRunProperties1);

    var run1 = new Run();

    var runProperties1 = new RunProperties()
    {
        
    };
    var noProof1 = new NoProof();

    runProperties1.Append(noProof1);
  
    runProperties1.AddChild(new SolidFill()
    {
        RgbColorModelHex = new RgbColorModelHex()
        {
            Val = new HexBinaryValue()
            {
                Value = "FF0000"
            }
        }
    });

    var drawing1 = new Drawing();

    var inline1 = new Inline
    {
        DistanceFromTop = (UInt32Value)0U, DistanceFromBottom = (UInt32Value)0U, DistanceFromLeft = (UInt32Value)0U,
        DistanceFromRight = (UInt32Value)0U, AnchorId = "1B7A2321", EditId = "37A25A4B"
    };
    var extent1 = new Extent { Cx = 1000000L, Cy = 666667L };
    var effectExtent1 = new EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 635L };
    var docProperties1 = new DocProperties { Id = (UInt32Value)827833609U, Name = "图片 1" };

    var nonVisualGraphicFrameDrawingProperties1 = new NonVisualGraphicFrameDrawingProperties();

    var graphicFrameLocks1 = new GraphicFrameLocks { NoChangeAspect = true };
    graphicFrameLocks1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

    nonVisualGraphicFrameDrawingProperties1.Append(graphicFrameLocks1);

    var graphic1 = new Graphic();
    graphic1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

    var graphicData1 = new GraphicData { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" };

    var picture1 = new Picture();
    picture1.AddNamespaceDeclaration("pic", "http://schemas.openxmlformats.org/drawingml/2006/picture");

    var nonVisualPictureProperties1 = new NonVisualPictureProperties();
    var nonVisualDrawingProperties1 = new NonVisualDrawingProperties { Id = (UInt32Value)827833609U, Name = "" };
    var nonVisualPictureDrawingProperties1 = new NonVisualPictureDrawingProperties();

    nonVisualPictureProperties1.Append(nonVisualDrawingProperties1);
    nonVisualPictureProperties1.Append(nonVisualPictureDrawingProperties1);

    var blipFill1 = new BlipFill();
    var blip1 = new Blip { Embed = idOfPart };

    var stretch1 = new Stretch();
    var fillRectangle1 = new FillRectangle();

    stretch1.Append(fillRectangle1);

    blipFill1.Append(blip1);
    blipFill1.Append(stretch1);

    var shapeProperties1 = new ShapeProperties();

    var transform2D1 = new Transform2D();
    var offset1 = new Offset { X = 0L, Y = 0L };
    var extents1 = new Extents { Cx = 1000000L, Cy = 666667L };

    transform2D1.Append(offset1);
    transform2D1.Append(extents1);

    var presetGeometry1 = new PresetGeometry { Preset = ShapeTypeValues.Rectangle };
    var adjustValueList1 = new AdjustValueList();

    presetGeometry1.Append(adjustValueList1);

    shapeProperties1.Append(transform2D1);
    shapeProperties1.Append(presetGeometry1);

    picture1.Append(nonVisualPictureProperties1);
    picture1.Append(blipFill1);
    picture1.Append(shapeProperties1);

    graphicData1.Append(picture1);

    graphic1.Append(graphicData1);

    inline1.Append(extent1);
    inline1.Append(effectExtent1);
    inline1.Append(docProperties1);
    inline1.Append(nonVisualGraphicFrameDrawingProperties1);
    inline1.Append(graphic1);

    drawing1.Append(inline1);

    run1.Append(runProperties1);
    run1.Append(drawing1);

    var run2 = new Run();

    var runProperties2 = new RunProperties();
    var runFonts2 = new RunFonts { Hint = FontTypeHintValues.EastAsia };
    var noProof2 = new NoProof();

    runProperties2.Append(runFonts2);
    runProperties2.Append(noProof2);
    var text1 = new Text { Space = SpaceProcessingModeValues.Preserve };
    text1.Text = " ";

    run2.Append(runProperties2);
    run2.Append(text1);

    var run3 = new Run();

    var runProperties3 = new RunProperties();
    var noProof3 = new NoProof();

    runProperties3.Append(noProof3);

    var drawing2 = new Drawing();

    var inline2 = new Inline
    {
        DistanceFromTop = (UInt32Value)0U, DistanceFromBottom = (UInt32Value)0U, DistanceFromLeft = (UInt32Value)0U,
        DistanceFromRight = (UInt32Value)0U, AnchorId = "00C5D8DF", EditId = "411D815C"
    };
    var extent2 = new Extent { Cx = 1000000L, Cy = 666667L };
    var effectExtent2 = new EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 635L };
    var docProperties2 = new DocProperties { Id = (UInt32Value)207470750U, Name = "图片 1" };

    var nonVisualGraphicFrameDrawingProperties2 = new NonVisualGraphicFrameDrawingProperties();

    var graphicFrameLocks2 = new GraphicFrameLocks { NoChangeAspect = true };
    graphicFrameLocks2.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

    nonVisualGraphicFrameDrawingProperties2.Append(graphicFrameLocks2);

    var graphic2 = new Graphic();
    graphic2.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

    var graphicData2 = new GraphicData { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" };

    var picture2 = new Picture();
    picture2.AddNamespaceDeclaration("pic", "http://schemas.openxmlformats.org/drawingml/2006/picture");

    var nonVisualPictureProperties2 = new NonVisualPictureProperties();
    var nonVisualDrawingProperties2 = new NonVisualDrawingProperties { Id = (UInt32Value)207470750U, Name = "" };
    var nonVisualPictureDrawingProperties2 = new NonVisualPictureDrawingProperties();

    nonVisualPictureProperties2.Append(nonVisualDrawingProperties2);
    nonVisualPictureProperties2.Append(nonVisualPictureDrawingProperties2);

    var blipFill2 = new BlipFill();
    var blip2 = new Blip { Embed = idOfPart };

    var stretch2 = new Stretch();
    var fillRectangle2 = new FillRectangle();

    stretch2.Append(fillRectangle2);

    blipFill2.Append(blip2);
    blipFill2.Append(stretch2);

    var shapeProperties2 = new ShapeProperties();

    var transform2D2 = new Transform2D();
    var offset2 = new Offset { X = 0L, Y = 0L };
    var extents2 = new Extents { Cx = 1000000L, Cy = 666667L };

    transform2D2.Append(offset2);
    transform2D2.Append(extents2);

    var presetGeometry2 = new PresetGeometry { Preset = ShapeTypeValues.Rectangle };
    var adjustValueList2 = new AdjustValueList();

    presetGeometry2.Append(adjustValueList2);

    shapeProperties2.Append(transform2D2);
    shapeProperties2.Append(presetGeometry2);

    picture2.Append(nonVisualPictureProperties2);
    picture2.Append(blipFill2);
    picture2.Append(shapeProperties2);

    graphicData2.Append(picture2);

    graphic2.Append(graphicData2);

    inline2.Append(extent2);
    inline2.Append(effectExtent2);
    inline2.Append(docProperties2);
    inline2.Append(nonVisualGraphicFrameDrawingProperties2);
    inline2.Append(graphic2);

    drawing2.Append(inline2);

    run3.Append(runProperties3);
    run3.Append(drawing2);

    paragraph.Append(paragraphProperties1);
    paragraph.Append(run1);
    paragraph.Append(run2);
    paragraph.Append(run3);

    tableCell.Append(tableCellProperties);
    tableCell.Append(paragraph);
    return tableCell;
}