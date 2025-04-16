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

using var document = SpreadsheetDocument.Open("Test.xlsx", isEditable: true);
CopyExcelSheet(document, "Sheet1", "Sheet2", 0);


void CopyExcelSheet(
    SpreadsheetDocument spreadsheetDocument,
    string sourceSheetName,
    string newSheetName,
    int insertPosition = -1)
{
    var workbookPart = spreadsheetDocument.WorkbookPart;

    var sourceSheet = workbookPart.Workbook.Sheets.Elements<Sheet>().First();

    var newSheet = (Sheet) sourceSheet.CloneNode(deep: true);
    newSheet.Name = newSheetName;
    newSheet.SheetId = (uint) (workbookPart.Workbook.Sheets.Count() + 1);

    //workbookPart.Workbook.Sheets.RemoveAllChildren();
    workbookPart.Workbook.Sheets.Append(newSheet);
}
