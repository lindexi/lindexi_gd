using System;

using (var presentationDocument =
       DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", true))
{
    presentationDocument.PackageProperties.Title = "Hello, World!ÄãºÃÊÀ½ç£¤&$#!";
}
// System.ArgumentException:¡°'', hexadecimal value 0x01, is an invalid character.¡±
//  	 System.Private.Xml.dll!System.Xml.XmlUtf8RawTextWriter.InvalidXmlChar(int ch, byte* pDst, bool entitize)	
//  	 System.Private.Xml.dll!System.Xml.XmlUtf8RawTextWriter.WriteElementTextBlock(char* pSrc, char* pSrcEnd)	
//  	 System.Private.Xml.dll!System.Xml.XmlUtf8RawTextWriter.WriteString(string text)	
//  	 System.Private.Xml.dll!System.Xml.XmlWellFormedWriter.WriteString(string text)	
//  	 System.IO.Packaging.dll!System.IO.Packaging.PartBasedPackageProperties.SerializeDirtyProperties()	
//  	 System.IO.Packaging.dll!System.IO.Packaging.PartBasedPackageProperties.Flush()	
//  	 System.IO.Packaging.dll!System.IO.Packaging.PartBasedPackageProperties.Close()	
//  	 System.IO.Packaging.dll!System.IO.Packaging.Package.System.IDisposable.Dispose()	
//  	 System.IO.Packaging.dll!System.IO.Packaging.Package.Close()	
//  	 DocumentFormat.OpenXml.Framework.dll!DocumentFormat.OpenXml.Features.StreamPackageFeature.Dispose(bool disposing)	
//  	 DocumentFormat.OpenXml.Framework.dll!DocumentFormat.OpenXml.Features.StreamPackageFeature.Dispose()	
//  	 DocumentFormat.OpenXml.Framework.dll!DocumentFormat.OpenXml.Packaging.PackageFeatureCollection.DocumentFormat.OpenXml.Features.IContainerDisposableFeature.Dispose()	
//  	 DocumentFormat.OpenXml.Framework.dll!DocumentFormat.OpenXml.Packaging.OpenXmlPackage.Dispose(bool disposing)	
//  	 DocumentFormat.OpenXml.Framework.dll!DocumentFormat.OpenXml.Packaging.OpenXmlPackage.Dispose()	
// >	 FukemqibairLaylalljerowhem.dll!Program.<Main>$(string[] args)
Console.WriteLine($"Finish");