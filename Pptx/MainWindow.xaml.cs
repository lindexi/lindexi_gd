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
            Debug.Assert(part.ContentType == "application/vnd.openxmlformats-officedocument.oleObject");

            var allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
            var s = part.GetStream();

            var byteArrayPool = new ByteArrayPool();
            var fakeStream = new ForwardSeekStream(s,byteArrayPool);
            var cf = new CompoundFile(fakeStream);
            var packageStream = cf.RootStorage.GetStream("Package"); 
            var tempFolder = @"F:\temp";
            if (!Directory.Exists(tempFolder))
            {
                tempFolder = System.IO.Path.GetTempPath();
            }
            var xlsxFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName() + ".xlsx");
            using (var fileStream = File.OpenWrite(xlsxFile))
            {
                fileStream.Write(packageStream.GetData().AsSpan());
            }
            using var spreadsheetDocument = SpreadsheetDocument.Open(xlsxFile, false);
            var sheets = spreadsheetDocument.WorkbookPart!.Workbook.Sheets;

            var lastAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
            Debug.WriteLine(lastAllocatedBytesForCurrentThread - allocatedBytesForCurrentThread);

      

            //// OpenMcdf.CFException:“Cannot load a non-seekable Stream”
            //var compoundFile = new CompoundFile(part.GetStream(FileMode.Open));

            var oleFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName());
            using (var fileStream = File.OpenWrite(oleFile))
            {
                using var stream = part.GetStream(FileMode.Open);
                stream.CopyTo(fileStream);
            }

            //var compoundFile = new CompoundFile(oleFile);
            //var packageStream = compoundFile.RootStorage.GetStream("Package");
            //var xlsxFile = System.IO.Path.Combine(tempFolder, System.IO.Path.GetRandomFileName() + ".xlsx");
            //using (var fileStream = File.OpenWrite(xlsxFile))
            //{
            //    fileStream.Write(packageStream.GetData().AsSpan());
            //}

            //using var spreadsheetDocument = SpreadsheetDocument.Open(xlsxFile, false);
            //var sheets = spreadsheetDocument.WorkbookPart!.Workbook.Sheets;


        }
    }

    public interface IByteArrayPool
    {
        byte[] Rent(int minimumLength);
        void Return(byte[] byteList);
    }

    public class ByteArrayPool : IByteArrayPool
    {
        public byte[] Rent(int minimumLength)
        {
            return ArrayPool<byte>.Shared.Rent(minimumLength);
        }

        public void Return(byte[] byteList)
        {
            ArrayPool<byte>.Shared.Return(byteList);
        }
    }

    public class ForwardSeekStream : Stream
    {
        public ForwardSeekStream(Stream sourceStream, IByteArrayPool byteArrayPool)
        {
            _sourceStream = sourceStream;
            _byteArrayPool = byteArrayPool;
        }

        public override void Flush()
        {
            _sourceStream.Flush();
        }

        public long CurrentPosition { private set; get; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _sourceStream.Read(buffer, offset, count);
            CurrentPosition += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset == CurrentPosition) return CurrentPosition;
            if (offset > CurrentPosition)
            {
                int length = (int) (offset - CurrentPosition);
                var byteList = _byteArrayPool.Rent(length);
                Read(byteList, 0, length);
                _byteArrayPool.Return(byteList);

                Debug.Assert(offset == CurrentPosition);
                return CurrentPosition;
            }

            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            _sourceStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _sourceStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _sourceStream.CanRead;

        public override bool CanSeek => _sourceStream.CanSeek;

        public override bool CanWrite => _sourceStream.CanWrite;

        public override long Length => _sourceStream.Length;

        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        private readonly Stream _sourceStream;
        private readonly IByteArrayPool _byteArrayPool;
    }
}
