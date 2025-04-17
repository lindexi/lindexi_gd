using System;
using System.Diagnostics;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;

using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualShapeProperties;

using var spreadsheetDocument = SpreadsheetDocument.Open("Test.xlsx", isEditable: true);
var workbookPart = spreadsheetDocument.WorkbookPart;
Sheet sheet = workbookPart.Workbook.Sheets.GetFirstChild<Sheet>();
WorksheetPart worksheetPart = (WorksheetPart) workbookPart.GetPartById(sheet.Id);
Worksheet worksheet = worksheetPart.Worksheet;
RowBreaks rowBreaks = worksheet.GetFirstChild<RowBreaks>();

;