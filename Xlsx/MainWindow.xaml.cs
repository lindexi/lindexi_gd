using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Xlsx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            FileInfo file = new("Test.xlsx");
            using var stream = file.OpenRead();
            SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(stream, false);

            var workbookPart = spreadsheet.WorkbookPart;
            Workbook workbook = workbookPart!.Workbook;
            var workbookView = workbook.BookViews?.GetFirstChild<WorkbookView>();
            var activeTabIndex = workbookView?.ActiveTab?.Value;
            Console.WriteLine($"当前激活的工作表序号：{activeTabIndex}");
            Debug.Assert(activeTabIndex != null); 
            // 通过序号去找到对应的工作表

            // 下面的获取方法是错误的，不能通过 WorksheetParts 的序号获取，原因是这里的顺序是依靠 workbook.xml.rels 文件里面存放的顺序决定的
            //var worksheetPart = workbookPart.WorksheetParts.ElementAt((int)activeTabIndex);

            // 正确的获取方法是 workbook.xml 的 Sheets 属性里面获取，这里的列表才是有序的
            var sheets = workbook.Sheets;
            // 序号从0开始
            var sheet = sheets!.Elements<Sheet>().ElementAt((int)activeTabIndex);

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!.Value!);
            var worksheet = worksheetPart.Worksheet;

            // 在工作表里面，也有一个属性表示当前是被选择的
            // 默认在 Excel 的行为就是被选择而且被激活
            var sheetViews = worksheet.SheetViews;
            var sheetView = sheetViews!.GetFirstChild<SheetView>();
            Console.WriteLine($"当前工作表被选择：{sheetView!.TabSelected}");
        }
    }

    public class LightTableElement : Grid
    {
        public LightTableElement()
        {

        }

        public int ColumnCount
        {
            get { return (int)GetValue(ColumnCountProperty); }
            set { SetValue(ColumnCountProperty, value); }
        }

        public static readonly DependencyProperty ColumnCountProperty =
            DependencyProperty.Register("ColumnCount", typeof(int), typeof(LightTableElement), GetFrameworkPropertyMetadata(0));

        public int RowCount
        {
            get { return (int)GetValue(RowCountProperty); }
            set { SetValue(RowCountProperty, value); }
        }

        public static readonly DependencyProperty RowCountProperty =
            DependencyProperty.Register("RowCount", typeof(int), typeof(LightTableElement), GetFrameworkPropertyMetadata(0));

        private static FrameworkPropertyMetadata GetFrameworkPropertyMetadata(object defaultValue) => new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsMeasure);
    }
}
