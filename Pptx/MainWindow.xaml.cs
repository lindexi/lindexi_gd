#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;

namespace Pptx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var file = new FileInfo("Test.pptx");

            using var presentationDocument = PresentationDocument.Open(file.FullName, false);
            var slide = presentationDocument.PresentationPart!.SlideParts.First().Slide;

            var graphicFrame = slide.CommonSlideData!.ShapeTree!.GetFirstChild<GraphicFrame>()!;
            var graphic = graphicFrame.Graphic!;
            var graphicData = graphic.GraphicData!;
            var alternateContent = graphicData.GetFirstChild<AlternateContent>()!;
            var choice = alternateContent.GetFirstChild<AlternateContentChoice>()!;
            var oleObject = choice.GetFirstChild<OleObject>()!;
            Debug.Assert(oleObject.GetFirstChild<OleObjectEmbed>() != null);
            var id = oleObject.Id!;
            var part = slide.SlidePart!.GetPartById(id!);
            Debug.Assert(part.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            // 预期字符串是 “Excel.Sheet.12” 等内容
            var isEmbedExcel = oleObject.ProgId?.Value?.StartsWith("Excel.Sheet", StringComparison.OrdinalIgnoreCase) is true;

            Debug.Assert(isEmbedExcel);

            var tempFolder = @"F:\temp";
            if (!Directory.Exists(tempFolder))
            {
                tempFolder = System.IO.Path.GetTempPath();
            }

            var xlsxFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName() + ".xlsx");
            using (var fileStream = File.OpenWrite(xlsxFile))
            {
                using var partStream = part.GetStream(FileMode.Open,FileAccess.Read);
                partStream.CopyTo(fileStream);
            }

            using var spreadsheetDocument = SpreadsheetDocument.Open(xlsxFile, false);
            var sheets = spreadsheetDocument.WorkbookPart!.Workbook.Sheets;


        }
    }
}
