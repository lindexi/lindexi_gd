using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace GanttWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Microsoft.Office.Interop.Excel.Application excel;
        Microsoft.Office.Interop.Excel.Workbook excelWorkBook;
        Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet;
        Microsoft.Office.Interop.Excel.Range excelCellRange;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateExcel(System.Data.DataTable DT)
        {
            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                excel.DisplayAlerts = false;
                excel.Visible = false;
                excelWorkBook = excel.Workbooks.Add(Type.Missing);
                excelWorkSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelWorkBook.ActiveSheet;
                excelWorkSheet.Name = "ExportToExcel";
                System.Data.DataTable dataTable = DT;
                excelWorkSheet.Cells.Font.Size = 11;
                int rowcount = 1;
                for (int i = 1; i <= dataTable.Columns.Count; i++) //For each column in DataTable  
                {
                    excelWorkSheet.Cells[1, i] = dataTable.Columns[i - 1].ColumnName;
                }
                foreach (System.Data.DataRow row in dataTable.Rows) //For each row in DataTable 
                {
                    rowcount += 1;
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        excelWorkSheet.Cells[rowcount, i + 1] = row[i].ToString();
                    }
                }
                excelCellRange = excelWorkSheet.Range[excelWorkSheet.Cells[1, 1], excelWorkSheet.Cells[rowcount, dataTable.Columns.Count]];
                excelCellRange.EntireColumn.AutoFit();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateExcel(Custom.GenerateDataTable<TaskDetails>(viewmodel.TaskDetails as ObservableCollection<TaskDetails>));
                excelWorkBook.SaveAs(System.IO.Path.Combine(@"D:", "GanttExcelExport"));
                MessageBox.Show("Exported Gantt data to excel");
                excelWorkBook.Close();
                excel.Quit();
            }
            catch (Exception)
            {
                MessageBox.Show("Saved as excel file");
            }
        }
    }
}
