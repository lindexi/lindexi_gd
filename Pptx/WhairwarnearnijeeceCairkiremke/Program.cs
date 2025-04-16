using System;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;

using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualShapeProperties;

using var document = SpreadsheetDocument.Open("Test.xlsx",isEditable:true);
CopyExcelSheet(document, "Sheet1", "Sheet2", 0);


WorksheetPart CopyExcelSheet(
    SpreadsheetDocument spreadsheetDocument,
    string sourceSheetName,
    string newSheetName,
    int insertPosition = -1)
{
    var workbookPart = spreadsheetDocument.WorkbookPart;

    // 查找源工作表
    var sourceSheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
        .FirstOrDefault(s => s.Name == sourceSheetName)
        ?? throw new Exception($"工作表 '{sourceSheetName}' 未找到.");

    

    var worksheetPart = (WorksheetPart) workbookPart.GetPartById(sourceSheet.Id);
    var sourceSheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
    var mergeCells = worksheetPart.Worksheet.Elements<MergeCells>().FirstOrDefault();
    var columns = worksheetPart.Worksheet.Elements<Columns>().FirstOrDefault(); // 获取列宽信息

    // 创建新工作表并设置新名称
    var newSheet = new Sheet
    {
        Name = newSheetName,
        SheetId = (uint) (workbookPart.Workbook.Sheets.Count() + 1),
    };

    newSheet = (Sheet) sourceSheet.CloneNode(deep: true);

    // 获取工作表集合并插入新工作表
    var sheets = workbookPart.Workbook.Sheets.Elements<Sheet>().ToList();
    var insertIndex = insertPosition < 0 || insertPosition >= sheets.Count() ? sheets.Count() : insertPosition;
    sheets.Insert(insertIndex, newSheet);

    // 更新工作簿中的 Sheets 元素
    workbookPart.Workbook.Sheets.RemoveAllChildren();
    workbookPart.Workbook.Sheets.Append(sheets);

    // 创建新工作表部分并初始化 Worksheet
    var newWorksheetPart = workbookPart.AddNewPart<WorksheetPart>();

    newSheet.Id = workbookPart.GetIdOfPart(newWorksheetPart);

    // 初始化一个新的 Worksheet 和 SheetData
    newWorksheetPart.Worksheet = new Worksheet(new SheetData());

    // 获取新工作表的 SheetData
    var newSheetData = newWorksheetPart.Worksheet.GetFirstChild<SheetData>();

    // 复制列宽（如果有的话）
    if (columns != null)
    {
        var newColumns = new Columns();
        foreach (var column in columns.Elements<Column>())
        {
            var newColumn = new Column()
            {
                Min = column.Min,
                Max = column.Max,
                Width = column.Width,
                CustomWidth = column.CustomWidth
            };
            newColumns.Append(newColumn);
        }
        newWorksheetPart.Worksheet.InsertAt(newColumns, 0); // 将列宽插入工作表
    }
    // 复制合并单元格
    if (mergeCells != null)
    {
        var newMergeCells = new MergeCells();
        foreach (var mergeCell in mergeCells.Elements<MergeCell>())
        {
            newMergeCells.Append(new MergeCell() { Reference = mergeCell.Reference });
        }
        newWorksheetPart.Worksheet.Append(newMergeCells);
    }

    // 深度克隆源工作表中的每一行并添加到新工作表
    foreach (var row in sourceSheetData.Elements<Row>())
    {
        var clonedRow = (Row) row.CloneNode(true);  // 克隆每一行

        // 如果原行有行高，则复制行高
        if (row.Height != null)
        {
            clonedRow.Height = row.Height;
            clonedRow.CustomHeight = row.CustomHeight;
        }

        newSheetData.Append(clonedRow);  // 将克隆的行添加到新工作表中
    }

    // 获取或创建共享字符串表
    var stringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault()
                           ?? workbookPart.AddNewPart<SharedStringTablePart>();

    // 确保共享字符串表已经更新
    int stringIndex = AddStringToSharedStringTable("测试插入新内容 \n 换行内容", stringTablePart);

    // 修改单元格并确保它们正确使用共享字符串索引
    foreach (var row in newSheetData.Elements<Row>())
    {
        foreach (var cell in row.Elements<Cell>())
        {
            if (cell.CellReference == "B22") // 仅修改指定单元格
            {
                // 更新单元格的数据类型为 SharedString，并且更新共享字符串索引
                cell.DataType = new EnumValue<DocumentFormat.OpenXml.Spreadsheet.CellValues>(DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString);
                cell.CellValue = new CellValue(stringIndex.ToString());
            }
        }
    }

    // 保存更改并返回新的工作表部分
    newWorksheetPart.Worksheet.Save();
    workbookPart.Workbook.Save(); // 确保 Workbook 保存

    return newWorksheetPart;
}

int AddStringToSharedStringTable(string test, SharedStringTablePart sharedStringTablePart)
{
    return 0;
}