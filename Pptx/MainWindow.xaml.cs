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
<<<<<<< HEAD
=======

using GeneratedCode;

using OpenMcdf;

>>>>>>> cd98a7a6b29e9297864aad9d7326a635b6b68e5b
using ColorMap = DocumentFormat.OpenXml.Presentation.ColorMap;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using Path = DocumentFormat.OpenXml.Drawing.Path;
using Rectangle = System.Windows.Shapes.Rectangle;
using SchemeColorValues = DocumentFormat.OpenXml.Drawing.SchemeColorValues;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using ShapeProperties = DocumentFormat.OpenXml.Presentation.ShapeProperties;
using Transform = System.Windows.Media.Transform;

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
<<<<<<< HEAD
            OpenPptxFile(new FileInfo("Test.pptx"));
        }

        private void File_OnDrop(object sender, DragEventArgs e)
        {
            string? fileName = ((System.Array)e.Data.GetData(DataFormats.FileDrop))?.GetValue(0)?.ToString();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            OpenPptxFile(new FileInfo(fileName));
        }

        private void OpenPptxFile(FileInfo file)
        {
            using var presentationDocument = PresentationDocument.Open(file.FullName, false);
            var slide = presentationDocument.PresentationPart!.SlideParts.First().Slide;
            // 这是对测试文件有要求的，要求一定传入的是立方体。请在具体的项目代码里面，替换为你需要的逻辑
            var shape = slide.Descendants<Shape>().First();
            var shapeProperties = shape.ShapeProperties;
            var presetGeometry = shapeProperties!.GetFirstChild<PresetGeometry>();
            var elementSize = new EmuSize(new Emu(1216152),new Emu(1216152));
            
            var shapePathList = GetPresetGeometryPath(presetGeometry!.Preset!.Value, elementSize);

            var drawingVisual = new DrawingGroup();
            var drawingContext = drawingVisual.Open();
            /*
                  <a:accent1>
                    <a:srgbClr val="4472C4" />
                  </a:accent1>
             */
            var fillBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#4472C4"));

            // 创建底色几何
            var pathGeometry = BuildShapePathGeometry(shapePathList);
            // 只有多路径下才先绘制底色
            drawingContext.DrawGeometry(fillBrush, null, pathGeometry);

            foreach (var shapePath in shapePathList)
            {
                if (!shapePath.IsFilled && !shapePath.IsStroke) continue;

                var geometry = GetPathGeometry(shapePath);
                if (geometry == null)
                {
                    continue;
                }

                var brush = GetShapeFillBrush(shapePath, fillBrush);
                // 忽略线条
                Pen? pen = null;
                drawingContext.DrawGeometry(brush, pen, geometry);
            }
            drawingContext.Close();

            var element = new Border()
            {
                Width = elementSize.Width.ToPixel().Value,
                Height = elementSize.Height.ToPixel().Value,

                Margin = new Thickness(10, 10, 10, 10),
                Background = new DrawingBrush(drawingVisual)
            };
            AddElement(element);
        }

        private SolidColorBrush? GetShapeFillBrush(ShapePath shapePath, SolidColorBrush fillBrush, bool isMultiPath=true)
        {
            if (shapePath.IsFilled is false)
            {
                return null;
            }

            switch (shapePath.FillMode)
            {
                case PathFillModeValues.Norm:
                {
                    if (isMultiPath)
                    {
                        // 多路径下，不需要重复绘制，绘制内容和底色相同。但是底色已绘制，因此啥都不用做
                        return null;
                    }
                    else
                    {
                        return fillBrush;
                    }
                }
                case PathFillModeValues.None:
                    return null;
                case PathFillModeValues.Darken:
                    return GetOrCreate("#64000000");
                case PathFillModeValues.DarkenLess:
                    return GetOrCreate("#32000000");
                case PathFillModeValues.Lighten:
                    return GetOrCreate("#64FFFFFF");
                case PathFillModeValues.LightenLess:
                    return GetOrCreate("#32FFFFFF");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            static SolidColorBrush GetOrCreate(string color)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
        }

        private void AddElement(UIElement element)
        {
            Grid.Children.Clear();
            Grid.Background = null;
            Grid.Children.Add(element);
        }

        private PathGeometry? GetPathGeometry(ShapePath shapePath)
        {
            var pathGeometry = new PathGeometry();
            var pathFigureCollection = new PathFigureCollection();
            var path = shapePath.Path;
            if (!string.IsNullOrEmpty(path))
            {
                var pathFigures = PathFigureCollection.Parse(path);
                if (!pathFigures.Any())
                {
                    return null;
                }
                foreach (var pathFigure in pathFigures)
                {
                    pathFigure.IsFilled = shapePath.IsFilled;
                    pathFigureCollection.Add(pathFigure);
                }
            }
            pathGeometry.Figures = pathFigureCollection;
            return pathGeometry;
=======
            foreach (var textAlignmentTypeValue in Enum.GetValues<TextAlignmentTypeValues>())
            {
                var generatedClass = new GeneratedClass()
                {
                    TextAlignment = textAlignmentTypeValue
                };

                var file = $"{textAlignmentTypeValue}.pptx";
                generatedClass.CreatePackage(file);

                Process.Start("explorer.exe", file);
            }
>>>>>>> cd98a7a6b29e9297864aad9d7326a635b6b68e5b
        }

        private PathGeometry BuildShapePathGeometry(ShapePath[] shapePathList)
        {
            PathGeometry? result = null;
            foreach (var shapePath in shapePathList)
            {
                if (shapePath.FillMode is PathFillModeValues.None && !shapePath.IsStroke)
                {
                    // 不是可填充的，而且不是线条的，啥都不做
                    continue;
                }

                var geometry = Geometry.Parse(shapePath.Path);
                if (result is null)
                {
                    result = PathGeometry.CreateFromGeometry(geometry);
                }
                else
                {
                    result = Geometry.Combine(result, geometry, GeometryCombineMode.Union, Transform.Identity);
                }
            }

            return result!;
        }

        private ShapePath[] GetPresetGeometryPath(ShapeTypeValues presetValue, EmuSize elementSize)
        {
            if (presetValue != ShapeTypeValues.Cube)
            {
                throw new ArgumentException($"本代码仅支持立方体");
            }

            var shapePathList = new ShapePath[4];

            // 没有想着使用 elementSize 哈，假设都是固定的大小
            // M 0.000,31.920 L 95.760,31.920 L 95.760,127.680 L 0.000,127.680 z
            // 		FillMode	Norm
            shapePathList[0] = new ShapePath("M 0.000,31.920 L 95.760,31.920 L 95.760,127.680 L 0.000,127.680 z",
                isStroke: false);

            // M 95.760,31.920 L 127.680,0.000 L 127.680,95.760 L 95.760,127.680 z
            // 		FillMode	DarkenLess	
            shapePathList[1] = new ShapePath("M 95.760,31.920 L 127.680,0.000 L 127.680,95.760 L 95.760,127.680 z", PathFillModeValues.DarkenLess, false);

            // M 0.000,31.920 L 31.920,0.000 L 127.680,0.000 L 95.760,31.920 z
            // 		FillMode	LightenLess	
            shapePathList[2] = new ShapePath("M 0.000,31.920 L 31.920,0.000 L 127.680,0.000 L 95.760,31.920 z", PathFillModeValues.LightenLess, false);

            // M 0.000,31.920 L 31.920,0.000 L 127.680,0.000 L 127.680,95.760 L 95.760,127.680 L 0.000,127.680 z M 0.000,31.920 L 95.760,31.920 L 127.680,0.000 M 95.760,31.920 L 95.760,127.680
            // 		FillMode	None	
            shapePathList[3] = new ShapePath("M 0.000,31.920 L 31.920,0.000 L 127.680,0.000 L 127.680,95.760 L 95.760,127.680 L 0.000,127.680 z M 0.000,31.920 L 95.760,31.920 L 127.680,0.000 M 95.760,31.920 L 95.760,127.680", PathFillModeValues.None);
            return shapePathList;
        }
    }

    /// <summary>
    /// 对应PPT的Shape Path
    /// </summary>
    public readonly struct ShapePath
    {
        /// <summary>
        /// 创建PPT的Geometry Path
        /// </summary>
        /// <param name="path">OpenXml  Path字符串</param>
        /// <param name="fillMode">OpenXml的Path Fill Mode  </param>
        /// <param name="isStroke">是否有轮廓</param>
        /// <param name="isExtrusionOk">指定使用 3D 拉伸可能在此路径</param>
        /// <param name="eumWidth">指定的宽度或在路径坐标系统中应在使用的最大的 x 坐标</param>
        /// <param name="eumHeight">指定框架的高度或在路径坐标系统中应在使用的最大的 y 坐标</param>
        public ShapePath(string path, PathFillModeValues fillMode = PathFillModeValues.Norm, bool isStroke = true, bool isExtrusionOk = false, double eumWidth = 0, double eumHeight = 0)
        {
            Path = path;
            IsStroke = isStroke;
            FillMode = fillMode;
            IsFilled = fillMode is not PathFillModeValues.None;
            IsExtrusionOk = isExtrusionOk;
            Width = new Emu(eumWidth);
            Height = new Emu(eumHeight);
        }

        /// <summary>
        /// 创建PPT的Geometry Path
        /// </summary>
        /// <param name="path">OpenXml  Path字符串</param>
        /// <param name="eumWidth">指定的宽度或在路径坐标系统中应在使用的最大的 x 坐标</param>
        /// <param name="eumHeight">指定框架的高度或在路径坐标系统中应在使用的最大的 y 坐标</param>
        public ShapePath(string path, double eumWidth, double eumHeight) : this(path, PathFillModeValues.Norm, eumWidth: eumWidth, eumHeight: eumHeight)
        {

        }

        /// <summary>
        /// 是否填充
        /// </summary>
        public bool IsFilled { get; }

        /// <summary>
        /// OpenXml 的 Path Stroke, 默认true
        /// </summary>
        public bool IsStroke { get; }

        /// <summary>
        /// OpenXml的Path Fill Mode  
        /// </summary>
        public PathFillModeValues FillMode { get; }

        /// <summary>
        ///OpenXml  Path字符串
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 指定使用 3D 拉伸可能在此路径，默认false或0
        /// </summary>
        public bool IsExtrusionOk { get; }

        /// <summary>
        /// 指定的宽度或在路径坐标系统中应在使用的最大的 x 坐标
        /// </summary>
        public Emu Width { get; }

        /// <summary>
        /// 指定框架的高度或在路径坐标系统中应在使用的最大的 y 坐标
        /// </summary>
        public Emu Height { get; }
    }
}
