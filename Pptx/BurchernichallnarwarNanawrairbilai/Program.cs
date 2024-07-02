// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

var file = @"C:\lindexi\Document\第二章分子的结构和性质.pptx";

using var presentationDocument = PresentationDocument.Open(file, true);

var presentationPartParts = presentationDocument.PresentationPart.Parts;

foreach (var presentationPartPart in presentationPartParts)
{
    if (presentationPartPart.OpenXmlPart.GetType().Name== "CommentAuthorsPart")
    {
        presentationDocument.PresentationPart.DeletePart(presentationPartPart.RelationshipId);
    }

    Console.WriteLine(presentationPartPart.OpenXmlPart.GetType());
}

var openXmlValidator = new OpenXmlValidator(FileFormatVersions.Office2010);
var validationErrorInfos = openXmlValidator.Validate(presentationDocument);

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