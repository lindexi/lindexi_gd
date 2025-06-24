// See https://aka.ms/new-console-template for more information
using MiniExcelLibs;
using MiniExcelLibs.Picture;

using System.Data;
using System.IO;

var imageFile = @"C:\lindexi\SplashScreen.png";
var outputExcelFile = "1.xlsx";
outputExcelFile = Path.GetFullPath(outputExcelFile);



var table = new DataTable();
{
    table.Columns.Add("Column1", typeof(string));
    table.Columns.Add("Column2", typeof(decimal));
    table.Rows.Add("MiniExcel", 1);
    table.Rows.Add("Github", 2);
}

MiniExcel.SaveAs(outputExcelFile, table, overwriteFile: true);

var images = new[]
{
    new MiniExcelPicture
    {
        ImageBytes = File.ReadAllBytes(imageFile),
        SheetName = null, // default null is first sheet
        CellAddress = "C3", // required
    },
};

MiniExcel.AddPicture(outputExcelFile, images);