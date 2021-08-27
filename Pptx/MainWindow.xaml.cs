#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using dotnetCampus.OpenXmlUnitConverter;
using ColorMap = DocumentFormat.OpenXml.Presentation.ColorMap;
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

            var shape = slide.CommonSlideData!.ShapeTree!.GetFirstChild<Shape>()!;
            /*
                  <p:sp>
                    <p:nvSpPr>
                      <p:cNvPr id="6" name="矩形 1" />
                      <p:cNvSpPr />
                      <p:nvPr />
                    </p:nvSpPr>
                    <p:spPr>
                      <a:xfrm>
                        <a:off x="3640346" y="1595887" />
                        <a:ext cx="3804249" cy="3071004" />
                      </a:xfrm>
                      <a:prstGeom prst="rect">
                        <a:avLst />
                      </a:prstGeom>
                      <a:ln w="76200" />
                    </p:spPr>
                    <p:style>
                      <a:lnRef idx="2">
                        <a:schemeClr val="accent1">
                          <a:shade val="50000" />
                        </a:schemeClr>
                      </a:lnRef>
                      <a:fillRef idx="1">
                        <a:schemeClr val="accent1" />
                      </a:fillRef>
                      <a:effectRef idx="0">
                        <a:schemeClr val="accent1" />
                      </a:effectRef>
                      <a:fontRef idx="minor">
                        <a:schemeClr val="lt1" />
                      </a:fontRef>
                    </p:style>
                    <p:txBody/>
                  </p:sp>
             */
            ShapeProperties shapeProperties = shape.ShapeProperties!;
            var presetGeometry = shapeProperties.GetFirstChild<PresetGeometry>()!;
            // 这是一份矩形的课件
            Debug.Assert(presetGeometry.Preset!.Value == ShapeTypeValues.Rectangle);

            // 虽然这个形状有轮廓，但是定义是 `<a:ln w="76200" />` 只有宽度，没有颜色
            Outline outline = shapeProperties.GetFirstChild<Outline>()!;
            Debug.Assert(outline.GetFirstChild<SchemeColor>() is null);
            var outlineWidth = new Emu(outline.Width!);

            // 实际的颜色应该从 `<a:lnRef idx="2">` 拿到
            var shapeStyle = shape.ShapeStyle!;
            var lineReference = shapeStyle.LineReference!;
            /*
                <a:lnRef idx="2">
                  <a:schemeClr val="accent1">
                    <a:shade val="50000" />
                  </a:schemeClr>
                </a:lnRef>
             */
            // 读取里层的颜色
            var schemeColor = lineReference.GetFirstChild<SchemeColor>()!;
            // 读取 SchemeColor 方法请参阅如下文档
            // [dotnet OpenXML 如何获取 schemeClr 颜色](https://blog.lindexi.com/post/dotnet-OpenXML-%E5%A6%82%E4%BD%95%E8%8E%B7%E5%8F%96-schemeClr-%E9%A2%9C%E8%89%B2.html )
            var colorMap = slide.GetColorMap()!;
            var colorScheme = slide.GetColorScheme()!;
            var value = schemeColor.Val!.Value; // accent1
            value = ColorHelper.SchemeColorMap(value, colorMap);
            var actualColor = ColorHelper.FindSchemeColor(value, colorScheme)!; // <a:srgbClr val="5B9BD5"/>
            RgbColorModelHex rgbColorModelHex = actualColor.RgbColorModelHex!;
            var color = (Color) ColorConverter.ConvertFromString($"#{rgbColorModelHex.Val!.Value}");
            // 根据 [dotnet OpenXML 颜色变换](https://blog.lindexi.com/post/dotnet-OpenXML-%E9%A2%9C%E8%89%B2%E5%8F%98%E6%8D%A2.html ) 进行修改颜色
            var shade = schemeColor.GetFirstChild<Shade>()!;// 让颜色变暗
            color = ColorTransform.HandleShade(color, shade);

            // 获取坐标
            var offset = shapeProperties.Transform2D!.Offset!;
            var x = new Emu(offset.X!);
            var y = new Emu(offset.Y!);
            var extents = shapeProperties.Transform2D.Extents!;
            var width = new Emu(extents.Cx!);
            var height = new Emu(extents.Cy!);

            // 创建元素
            var rectangle = new Rectangle()
            {
                Margin = new Thickness(x.ToPixel().Value, y.ToPixel().Value, 0, 0),
                Width = width.ToPixel().Value,
                Height = height.ToPixel().Value,
                StrokeThickness = outlineWidth.ToPixel().Value,
                Stroke = new SolidColorBrush(color)
            };

            Canvas.Children.Add(rectangle);
        }
    }

    static class ColorTransform
    {
        public static Color HandleShade(Color color, Shade? shadeElement)
        {
            var updatedColor = color;
            if (shadeElement is not null)
            {
                var shadeVal = shadeElement.Val;
                var shade = shadeVal is not null ? new Percentage(shadeVal) : Percentage.FromDouble(1);
                var linearR = SRgbToLinearRgb(updatedColor.R / 255.0);
                var linearG = SRgbToLinearRgb(updatedColor.G / 255.0);
                var linearB = SRgbToLinearRgb(updatedColor.B / 255.0);
                var r = linearR * shade.DoubleValue;
                var g = linearG * shade.DoubleValue;
                var b = linearB * shade.DoubleValue;
                updatedColor.R = (byte)Math.Round(255 * LinearRgbToSRgb(r));
                updatedColor.G = (byte)Math.Round(255 * LinearRgbToSRgb(g));
                updatedColor.B = (byte)Math.Round(255 * LinearRgbToSRgb(b));
            }

            return updatedColor;
        }

        /// <summary>
        ///     https://en.wikipedia.org/wiki/SRGB#The_forward_transformation_.28CIE_xyY_or_CIE_XYZ_to_sRGB.29
        /// </summary>
        /// <param name="sRgb"></param>
        /// <returns></returns>
        private static double SRgbToLinearRgb(double sRgb)
        {
            if (sRgb <= 0.04045) return sRgb / 12.92;

            return Math.Pow((sRgb + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        ///     https://en.wikipedia.org/wiki/SRGB#The_forward_transformation_.28CIE_xyY_or_CIE_XYZ_to_sRGB.29
        /// </summary>
        /// <param name="linearRgb"></param>
        /// <returns></returns>
        private static double LinearRgbToSRgb(double linearRgb)
        {
            if (linearRgb < 0.0031308) return 12.92 * linearRgb;

            //var linearR=3.24096994*sR-1.53738318*sg-0.49861076*sb
            return Math.Pow(linearRgb, 1.0 / 2.4) * 1.055 - 0.055;
        }
    }

    static class ColorHelper
    {
        public static Color2Type? FindSchemeColor(SchemeColorValues value, ColorScheme scheme)
        {
            return value switch
            {
                SchemeColorValues.Accent1 => scheme.Accent1Color,
                SchemeColorValues.Accent2 => scheme.Accent2Color,
                SchemeColorValues.Accent3 => scheme.Accent3Color,
                SchemeColorValues.Accent4 => scheme.Accent4Color,
                SchemeColorValues.Accent5 => scheme.Accent5Color,
                SchemeColorValues.Accent6 => scheme.Accent6Color,
                SchemeColorValues.Dark1 => scheme.Dark1Color,
                SchemeColorValues.Dark2 => scheme.Dark2Color,
                SchemeColorValues.FollowedHyperlink => scheme.FollowedHyperlinkColor,
                SchemeColorValues.Hyperlink => scheme.Hyperlink,
                SchemeColorValues.Light1 => scheme.Light1Color,
                SchemeColorValues.Light2 => scheme.Light2Color,
                _ => null
            };
        }

        public static SchemeColorValues SchemeColorMap(SchemeColorValues value, ColorMap map)
        {
            return value switch
            {
                SchemeColorValues.Accent1 => IndexToSchemeColor(map.Accent1),
                SchemeColorValues.Accent2 => IndexToSchemeColor(map.Accent2),
                SchemeColorValues.Accent3 => IndexToSchemeColor(map.Accent3),
                SchemeColorValues.Accent4 => IndexToSchemeColor(map.Accent4),
                SchemeColorValues.Accent5 => IndexToSchemeColor(map.Accent5),
                SchemeColorValues.Accent6 => IndexToSchemeColor(map.Accent6),
                SchemeColorValues.Dark1 => SchemeColorValues.Dark1,
                SchemeColorValues.Dark2 => SchemeColorValues.Dark2,
                SchemeColorValues.FollowedHyperlink => IndexToSchemeColor(map.FollowedHyperlink),
                SchemeColorValues.Hyperlink => IndexToSchemeColor(map.Hyperlink),
                SchemeColorValues.Light1 => SchemeColorValues.Light1,
                SchemeColorValues.Light2 => SchemeColorValues.Light2,
                SchemeColorValues.Background1 => IndexToSchemeColor(map.Background1),
                SchemeColorValues.Background2 => IndexToSchemeColor(map.Background2),
                SchemeColorValues.Text1 => IndexToSchemeColor(map.Text1),
                SchemeColorValues.Text2 => IndexToSchemeColor(map.Text2),
                _ => SchemeColorValues.Accent1
            };
        }

        private static SchemeColorValues IndexToSchemeColor(EnumValue<ColorSchemeIndexValues>? value)
        {
            if (value is null)
            {
                return SchemeColorValues.Accent1;
            }

            var colorSchemeIndexValue = value.Value;

            return colorSchemeIndexValue switch
            {
                ColorSchemeIndexValues.Accent1 => SchemeColorValues.Accent1,
                ColorSchemeIndexValues.Accent2 => SchemeColorValues.Accent2,
                ColorSchemeIndexValues.Accent3 => SchemeColorValues.Accent3,
                ColorSchemeIndexValues.Accent4 => SchemeColorValues.Accent4,
                ColorSchemeIndexValues.Accent5 => SchemeColorValues.Accent5,
                ColorSchemeIndexValues.Accent6 => SchemeColorValues.Accent6,
                ColorSchemeIndexValues.Dark1 => SchemeColorValues.Dark1,
                ColorSchemeIndexValues.Dark2 => SchemeColorValues.Dark2,
                ColorSchemeIndexValues.FollowedHyperlink => SchemeColorValues.FollowedHyperlink,
                ColorSchemeIndexValues.Hyperlink => SchemeColorValues.Hyperlink,
                ColorSchemeIndexValues.Light1 => SchemeColorValues.Light1,
                ColorSchemeIndexValues.Light2 => SchemeColorValues.Light2,
                _ => SchemeColorValues.Accent1
            };
        }
    }

    static class RootElementExtensions
    {
        public static ColorScheme? GetColorScheme(this OpenXmlPartRootElement root)
        {
            var (slidePart, slideLayoutPart, slideMasterPart) = GetParts(root);

            //从当前Slide获取theme
            if (slidePart?.ThemeOverridePart?.ThemeOverride.ColorScheme != null)
                return slidePart.ThemeOverridePart.ThemeOverride.ColorScheme;

            //从SlideLayout获取theme
            if (slideLayoutPart?.ThemeOverridePart?.ThemeOverride.ColorScheme != null)
                return slideLayoutPart.ThemeOverridePart.ThemeOverride.ColorScheme;

            //从SlideMaster获取theme
            return slideMasterPart?.ThemePart?.Theme.ThemeElements?.ColorScheme;
        }

        public static ColorMap? GetColorMap(this OpenXmlPartRootElement root)
        {
            var (slidePart, slideLayoutPart, slideMasterPart) = GetParts(root);

            var masterColorMap = slideMasterPart?.SlideMaster.ColorMap;

            //从当前Slide获取ColorMap
            if (slidePart?.Slide.ColorMapOverride != null)
            {
                if (slidePart.Slide.ColorMapOverride.MasterColorMapping != null) return masterColorMap;

                if (slidePart.Slide.ColorMapOverride.OverrideColorMapping != null)
                    return slidePart.Slide.ColorMapOverride.OverrideColorMapping.ToColorMap();
            }

            //从SlideLayout获取ColorMap
            if (slideLayoutPart?.SlideLayout.ColorMapOverride != null)
            {
                if (slideLayoutPart.SlideLayout.ColorMapOverride.MasterColorMapping != null) return masterColorMap;

                if (slideLayoutPart.SlideLayout.ColorMapOverride.OverrideColorMapping != null)
                    return slideLayoutPart.SlideLayout.ColorMapOverride.OverrideColorMapping.ToColorMap();
            }

            //从SlideMaster获取ColorMap
            return masterColorMap;
        }

        private static (SlidePart? slidePart, SlideLayoutPart? slideLayoutPart, SlideMasterPart? slideMasterPart) GetParts(OpenXmlPartRootElement root)
        {
            SlidePart? slidePart = null;
            SlideLayoutPart? slideLayoutPart = null;
            SlideMasterPart? slideMasterPart = null;
            if (root is Slide slide) slidePart = slide.SlidePart;

            if (slidePart != null)
                slideLayoutPart = slidePart.SlideLayoutPart;
            else if (root is SlideLayout slideLayout) slideLayoutPart = slideLayout.SlideLayoutPart;

            if (slideLayoutPart != null)
                slideMasterPart = slideLayoutPart.SlideMasterPart;
            else if (root is SlideMaster slideMaster) slideMasterPart = slideMaster.SlideMasterPart;

            return (slidePart, slideLayoutPart, slideMasterPart);
        }

        /// <summary>
        ///     将<see cref="OverrideColorMapping" />转换为<see cref="ColorMap" />
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public static ColorMap ToColorMap(this OverrideColorMapping mapping)
        {
            return new ColorMap
            {
                Accent1 = mapping.Accent1,
                Accent2 = mapping.Accent2,
                Accent3 = mapping.Accent3,
                Accent4 = mapping.Accent4,
                Accent5 = mapping.Accent5,
                Accent6 = mapping.Accent6,
                Background1 = mapping.Background1,
                Background2 = mapping.Background2,
                FollowedHyperlink = mapping.FollowedHyperlink,
                Hyperlink = mapping.Hyperlink,
                Text1 = mapping.Text1,
                Text2 = mapping.Text2
            };
        }
    }
}
