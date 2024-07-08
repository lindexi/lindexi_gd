// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

var file = @"E:\download\E盘下载器\url1036088627241172992.pptx.docx";

using var presentationDocument = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(file, true);

//var presentationPartParts = presentationDocument.PresentationPart.Parts;

//foreach (var presentationPartPart in presentationPartParts)
//{
//    if (presentationPartPart.OpenXmlPart.GetType().Name == "CommentAuthorsPart")
//    {
//        presentationDocument.PresentationPart.DeletePart(presentationPartPart.RelationshipId);
//    }

//    if (presentationPartPart.OpenXmlPart.GetType().Name == "SlidePart")
//    {
//        presentationDocument.PresentationPart.DeletePart(presentationPartPart.RelationshipId);
//    }
//    Console.WriteLine(presentationPartPart.OpenXmlPart.GetType());
//}

var openXmlValidator = new OpenXmlValidator(FileFormatVersions.Microsoft365);
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