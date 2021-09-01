#nullable enable
using System;
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
using ColorMap = DocumentFormat.OpenXml.Presentation.ColorMap;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
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
            var table = graphicData.GetFirstChild<Table>()!; // a:tbl
            /*
               <a:tbl>
                 <a:tr h="370840">
                   <a:tc rowSpan="2">
                     <a:txBody>
                       <a:bodyPr />
                       <a:lstStyle />
                       <a:p>
                         <a:r>
                           <a:rPr lang="en-US" altLang="zh-CN" dirty="0" smtClean="0" />
                           <a:t>123123</a:t>
                         </a:r>
                         <a:endParaRPr lang="zh-CN" altLang="en-US" dirty="0" />
                       </a:p>
                     </a:txBody>
                     <a:tcPr />
                   </a:tc>
                   <a:tc></a:tc>
                 </a:tr>
                 <a:tr h="370840">
                   <a:tc vMerge="1">
                     <a:txBody>
                       <a:bodyPr />
                       <a:lstStyle />
                       <a:p>
                         <a:r>
                           <a:rPr lang="en-US" altLang="zh-CN" smtClean="0" />
                           <a:t>投毒</a:t>
                         </a:r>
                         <a:endParaRPr lang="zh-CN" altLang="en-US" />
                       </a:p>
                     </a:txBody>
                     <a:tcPr />
                   </a:tc>
                   <a:tc></a:tc>
                 </a:tr>
               </a:tbl>
             */

            var dataTable = new DataTable();
            DataGrid.DataContext = dataTable;
            DataGrid.HeadersVisibility = DataGridHeadersVisibility.None;

            var n = 0;
            foreach (var gridColumn in table.TableGrid!.Elements<GridColumn>())
            {
                var emu = new Emu(gridColumn.Width?.Value ?? 95250);

                DataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Width = emu.ToPixel().Value,
                    Binding = new Binding(n.ToString())
                });

                dataTable.Columns.Add(n.ToString());
                n++;
            }

            foreach (var openXmlElement in table)
            {
                // a:tr 表格的行
                if (openXmlElement is TableRow tableRow)
                {
                    var dataRow = dataTable.NewRow();
                    dataTable.Rows.Add(dataRow);

                    var index = 0;
                    foreach (var tableCell in tableRow.Elements<TableCell>())
                    {
                        var text = tableCell.TextBody!.InnerText;
                        dataRow[index.ToString()] = text;

                        index++;
                    }
                }
            }
        }
    }
}
