using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.Text;

namespace JiyinunalcheWaqerehoqarlijear;

class PowerPointReader
{
    public async Task<IReadOnlyList<PowerPointSlideInfo>> ReadSlidesAsync(FileInfo pptxFile)
    {
        ArgumentNullException.ThrowIfNull(pptxFile);
        if (!pptxFile.Exists)
            throw new FileNotFoundException("PPTX 文件不存在", pptxFile.FullName);

        // 1. 读取所有幻灯片文本和结构
        var slideInfos = new List<(int Index, string Text)>();
        using (var presentation = PresentationDocument.Open(pptxFile.FullName, false))
        {
            var presentationPart = presentation.PresentationPart;
            if (presentationPart == null || presentationPart.Presentation == null)
                throw new InvalidOperationException("PPTX 文件结构无效");

            var slideIdList = presentationPart.Presentation.SlideIdList;
            if (slideIdList == null)
                throw new InvalidOperationException("未找到幻灯片列表");

            int slideIndex = 1;
            foreach (var slideId in slideIdList.Elements<SlideId>())
            {
                var relId = slideId.RelationshipId;
                var slidePart = (SlidePart)presentationPart.GetPartById(relId!);
                var slide = slidePart.Slide;
                var sb = new StringBuilder();

                // 标题
                var titleShape = slide.Descendants<Shape>().FirstOrDefault(s =>
                    s.ShapeProperties == null && s.TextBody != null &&
                    s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.Contains("标题") == true);
                if (titleShape != null)
                {
                    var titleText = GetShapeText(titleShape);
                    if (!string.IsNullOrWhiteSpace(titleText))
                        sb.AppendLine($"[标题] {titleText}");
                }

                // 所有文本框
                foreach (var shape in slide.Descendants<Shape>())
                {
                    var text = GetShapeText(shape);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var isTitle = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.Contains("标题") == true;
                    if (!isTitle)
                        sb.AppendLine($"[文本框] {text}");
                }

                // 表格
                foreach (var table in slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>())
                {
                    sb.AppendLine("[表格]");
                    foreach (var row in table.Descendants<DocumentFormat.OpenXml.Drawing.TableRow>())
                    {
                        var rowText = string.Join(" | ", row.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>().Select(cell => GetParagraphsText(cell.TextBody)));
                        sb.AppendLine(rowText);
                    }
                }

                // 备注
                if (slidePart.NotesSlidePart != null)
                {
                    var notesSlide = slidePart.NotesSlidePart.NotesSlide;
                    // NotesSlide 的所有 Paragraph
                    var notesText = GetNotesSlideText(notesSlide);
                    if (!string.IsNullOrWhiteSpace(notesText))
                        sb.AppendLine($"[备注] {notesText}");
                }

                slideInfos.Add((slideIndex, sb.ToString().Trim()));
                slideIndex++;
            }
        }

        // 2. 导出每页截图
        var outputDir = Path.Combine(Path.GetTempPath(), $"pptx_{Guid.NewGuid():N}");
        var imagePaths = await PowerPresentationProvider.ExportSlideImagesAsync(pptxFile, outputDir, null, CancellationToken.None);

        // 3. 组装 PowerPointSlideInfo
        var result = new List<PowerPointSlideInfo>();
        for (int i = 0; i < slideInfos.Count; i++)
        {
            var slideIndex = slideInfos[i].Index;
            var slideText = slideInfos[i].Text;
            var imageFile = new FileInfo(imagePaths.Count > i ? imagePaths[i] : string.Empty);
            result.Add(new PowerPointSlideInfo(slideIndex, slideText, imageFile));
        }
        return result;
    }


    private static string GetShapeText(Shape shape)
    {
        // shape.TextBody 可能是 Presentation.TextBody，需遍历其所有 Drawing.Paragraph
        if (shape.TextBody == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var para in shape.TextBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = string.Concat(para.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }
        return sb.ToString().Trim();
    }

    private static string GetNotesSlideText(NotesSlide notesSlide)
    {
        if (notesSlide == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var para in notesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = string.Concat(para.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }
        return sb.ToString().Trim();
    }

    private static string GetParagraphsText(DocumentFormat.OpenXml.Drawing.TextBody? textBody)
    {
        if (textBody == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var para in textBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = string.Concat(para.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }
        return sb.ToString().Trim();
    }
}

record PowerPointSlideInfo(int SlideIndex, string SlideText, FileInfo SlideImageFile);
