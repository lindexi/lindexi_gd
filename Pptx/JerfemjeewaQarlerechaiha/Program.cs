// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

var file = @"C:\lindexi\Document\1.docx";

var wordprocessingDocument = WordprocessingDocument.Open(file,false);
var openXmlValidator = new OpenXmlValidator(FileFormatVersions.Microsoft365);
var validationErrorInfos = openXmlValidator.Validate(wordprocessingDocument);

var index = 0;
foreach (var validationErrorInfo in validationErrorInfos)
{
    if (validationErrorInfo.Description.Contains("attribute is not declared"))
    {
        continue;
    }

    Console.WriteLine(
        $"""
        第 {index} 个错误：
        错误描述： {validationErrorInfo.Description}
        出错文件：{validationErrorInfo.Part?.Uri}
        XPath 路径：{validationErrorInfo.Path?.XPath}
        """);
    index++;
}

Console.WriteLine("Hello, World!");
