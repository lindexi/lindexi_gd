using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace JawjeleceeYairlubelhearrene;

internal sealed class PowerPointReader
{
    public async Task<PowerPointReadResult> ReadSlidesAsync(System.IO.FileInfo pptxFile, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pptxFile);
        if (!pptxFile.Exists)
        {
            throw new FileNotFoundException("PPT 文件不存在。", pptxFile.FullName);
        }

        progress?.Report("正在读取 PPT 文本内容...");
        var slideInfos = ReadSlidesText(pptxFile, cancellationToken);

        progress?.Report("正在通过 PowerPoint 导出页面截图...");
        var outputDir = Path.Combine(Path.GetTempPath(), $"pptx_{Guid.NewGuid():N}");
        IReadOnlyList<string> imagePaths;

        try
        {
            imagePaths = await PowerPresentationProvider.ExportSlideImagesAsync(pptxFile, outputDir, progress, cancellationToken);
        }
        catch (COMException exception)
        {
            throw new InvalidOperationException("调用 PowerPoint COM 失败，请确认设备已安装可用的 Microsoft PowerPoint。", exception);
        }
        catch (FileNotFoundException exception) when (exception.Message.Contains("office", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前环境缺少 PowerPoint 所需组件，请确认 Office 安装完整。", exception);
        }

        var result = new List<PowerPointSlideInfo>(slideInfos.Count);
        for (var i = 0; i < slideInfos.Count; i++)
        {
            var slide = slideInfos[i];
            var imageFilePath = imagePaths.Count > i ? imagePaths[i] : string.Empty;
            result.Add(new PowerPointSlideInfo(slide.Index, slide.Text, new System.IO.FileInfo(imageFilePath)));
        }

        progress?.Report($"已完成 PPT 读取，共 {result.Count} 页。");
        return new PowerPointReadResult(pptxFile, result);
    }

    private static List<(int Index, string Text)> ReadSlidesText(System.IO.FileInfo pptxFile, CancellationToken cancellationToken)
    {
        var slideInfos = new List<(int Index, string Text)>();

        using var presentation = PresentationDocument.Open(pptxFile.FullName, false);
        var presentationPart = presentation.PresentationPart;
        if (presentationPart?.Presentation is null)
        {
            throw new InvalidOperationException("PPT 文件结构无效。");
        }

        var slideIdList = presentationPart.Presentation.SlideIdList;
        if (slideIdList is null)
        {
            throw new InvalidOperationException("未找到幻灯片列表。");
        }

        var slideIndex = 1;
        foreach (var slideId in slideIdList.Elements<SlideId>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relationshipId = slideId.RelationshipId;
            var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId!);
            var slide = slidePart.Slide;
            var stringBuilder = new StringBuilder();

            var titleShape = slide.Descendants<Shape>().FirstOrDefault(shape =>
                shape.ShapeProperties == null &&
                shape.TextBody is not null &&
                shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.Contains("标题", StringComparison.Ordinal) == true);
            if (titleShape is not null)
            {
                var titleText = GetShapeText(titleShape);
                if (!string.IsNullOrWhiteSpace(titleText))
                {
                    stringBuilder.AppendLine($"[标题] {titleText}");
                }
            }

            foreach (var shape in slide.Descendants<Shape>())
            {
                var text = GetShapeText(shape);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var isTitle = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.Contains("标题", StringComparison.Ordinal) == true;
                if (!isTitle)
                {
                    stringBuilder.AppendLine($"[文本框] {text}");
                }
            }

            foreach (var table in slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>())
            {
                stringBuilder.AppendLine("[表格]");
                foreach (var row in table.Descendants<DocumentFormat.OpenXml.Drawing.TableRow>())
                {
                    var rowText = string.Join(" | ", row.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>().Select(cell => GetParagraphsText(cell.TextBody)));
                    stringBuilder.AppendLine(rowText);
                }
            }

            if (slidePart.NotesSlidePart?.NotesSlide is { } notesSlide)
            {
                var notesText = GetNotesSlideText(notesSlide);
                if (!string.IsNullOrWhiteSpace(notesText))
                {
                    stringBuilder.AppendLine($"[备注] {notesText}");
                }
            }

            slideInfos.Add((slideIndex, stringBuilder.ToString().Trim()));
            slideIndex++;
        }

        return slideInfos;
    }

    private static string GetShapeText(Shape shape)
    {
        if (shape.TextBody is null)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        foreach (var paragraph in shape.TextBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                stringBuilder.AppendLine(text);
            }
        }

        return stringBuilder.ToString().Trim();
    }

    private static string GetNotesSlideText(NotesSlide notesSlide)
    {
        var stringBuilder = new StringBuilder();
        foreach (var paragraph in notesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                stringBuilder.AppendLine(text);
            }
        }

        return stringBuilder.ToString().Trim();
    }

    private static string GetParagraphsText(DocumentFormat.OpenXml.Drawing.TextBody? textBody)
    {
        if (textBody is null)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        foreach (var paragraph in textBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                stringBuilder.AppendLine(text);
            }
        }

        return stringBuilder.ToString().Trim();
    }
}
