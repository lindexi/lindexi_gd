#nullable enable
using System;
using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using dotnetCampus.OpenXmlUnitConverter;
using OpenMcdf;
using ColorMap = DocumentFormat.OpenXml.Presentation.ColorMap;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using Path = DocumentFormat.OpenXml.Drawing.Path;
using Rectangle = System.Windows.Shapes.Rectangle;
using SchemeColorValues = DocumentFormat.OpenXml.Drawing.SchemeColorValues;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using ShapeProperties = DocumentFormat.OpenXml.Presentation.ShapeProperties;

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

            //var file = @"F:\temp\foo" + (char) 1+".txt";
            //File.WriteAllText(file, "123");
            using var s = Foo();
        }

        private Stream Foo()
        {
            return null;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var file = new FileInfo("Test.pptx");

            var tf = @"F:\temp\KewalnaidiNaborereefal.pptx";
            if (File.Exists(tf))
            {
                file = new FileInfo(tf);
            }

            long lastAllocatedBytesForCurrentThread;

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
            Debug.Assert(part.ContentType == "application/vnd.openxmlformats-officedocument.oleObject");

            var allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
            var s = part.GetStream();

            lastAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
            Debug.WriteLine($"GetStream {lastAllocatedBytesForCurrentThread - allocatedBytesForCurrentThread}");
            allocatedBytesForCurrentThread = lastAllocatedBytesForCurrentThread;

            var tempFolder = @"F:\temp";
            if (!Directory.Exists(tempFolder))
            {
                tempFolder = System.IO.Path.GetTempPath();
            }

            tempFolder = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);
            //CompoundFileUnzipper.Unzip(s, tempFolder, byteArrayPool);

            // 为了性能考虑，不再使用内存方式，全部写入到文件
            //var forwardSeekStream = new ForwardSeekStream(s, byteArrayPool);
            var oleFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName());
            using var oleFileStream = new FileStream(oleFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                bufferSize: 4096, FileOptions.RandomAccess);
            s.CopyTo(oleFileStream);
            oleFileStream.Position = 0;

            //Origin(forwardSeekStream);
            var cf = new ReadonlyCompoundFile(oleFileStream);

            lastAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
            Debug.WriteLine($"CompoundFile {lastAllocatedBytesForCurrentThread - allocatedBytesForCurrentThread}");
            allocatedBytesForCurrentThread = lastAllocatedBytesForCurrentThread;

            var packageStream = cf.RootStorage.GetStream("Package");

            lastAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
            Debug.WriteLine($"GetStream {lastAllocatedBytesForCurrentThread - allocatedBytesForCurrentThread}");
            allocatedBytesForCurrentThread = lastAllocatedBytesForCurrentThread;

            //var tempFolder = @"F:\temp";
            //if (!Directory.Exists(tempFolder))
            //{
            //    tempFolder = System.IO.Path.GetTempPath();
            //}

            var xlsxFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName() + ".xlsx");
            using (var fileStream = File.OpenWrite(xlsxFile))
            {
                //fileStream.Write(packageStream.GetData().AsSpan());
                cf.CopyTo(packageStream, fileStream);
            }

            //var fakeStream = new FakeStream();
            //cf.CopyTo(packageStream, fakeStream, byteArrayPool);

            //using (var fileStream = File.OpenWrite(xlsxFile))
            //{
            //    fileStream.Write(packageStream.GetData().AsSpan());
            //}
            //using var spreadsheetDocument = SpreadsheetDocument.Open(xlsxFile, false);
            //var sheets = spreadsheetDocument.WorkbookPart!.Workbook.Sheets;

            lastAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
            Debug.WriteLine($"CopyTo {lastAllocatedBytesForCurrentThread - allocatedBytesForCurrentThread}");
            allocatedBytesForCurrentThread = lastAllocatedBytesForCurrentThread;



            //// OpenMcdf.CFException:“Cannot load a non-seekable Stream”
            //var compoundFile = new CompoundFile(part.GetStream(FileMode.Open));

            //var oleFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName());
            //using (var fileStream = File.OpenWrite(oleFile))
            //{
            //    using var stream = part.GetStream(FileMode.Open);
            //    stream.CopyTo(fileStream);
            //}

            //var compoundFile = new CompoundFile(oleFile);
            //var packageStream = compoundFile.RootStorage.GetStream("Package");
            //var xlsxFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName() + ".xlsx");
            //using (var fileStream = File.OpenWrite(xlsxFile))
            //{
            //    fileStream.Write(packageStream.GetData().AsSpan());
            //}

            using var spreadsheetDocument = SpreadsheetDocument.Open(xlsxFile, false);
            var sheets = spreadsheetDocument.WorkbookPart!.Workbook.Sheets;


        }
    }

    class FakeStream : Stream
    {
        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
    }
}
