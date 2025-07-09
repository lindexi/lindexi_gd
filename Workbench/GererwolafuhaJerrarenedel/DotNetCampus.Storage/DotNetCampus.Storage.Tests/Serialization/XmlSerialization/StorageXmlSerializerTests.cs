using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetCampus.Storage.Lib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DotNetCampus.Storage.Lib.Tests;

[TestClass()]
public class StorageXmlSerializerTests
{
    [TestMethod()]
    public async Task ParseTest()
    {
        var testFile = @"C:\lindexi\slide1.xml";

        using var fileStream = File.OpenRead(testFile);
        var document = await XDocument.LoadAsync(fileStream, LoadOptions.None,CancellationToken.None);

        //XmlReader xmlReader = XmlReader.Create(fileStream);
        // XML 文档内容：
        /*
        <Document Type="Element">
        </Document>
         */
    }

    [TestMethod()]
    public void ParseTest2()
    {
        // lang=xml
        var xmlDocument =
            """
            <Document a:Type="Element" xmlns:a="Test">
              <ElementList a:Type="Property">
                <Picture a:Type="Element" X="100" Y="100">
                  <Fill a:Type="Property">
                     <Blip a:Type="Element" Source="Fooxcxszczxc"/>
                  </Fill>
                  <!-- <PresetMode a:Type="Property"> -->
                  <!--   <Effect a:Type="Property"> -->
                  <!--     <EffectMode a:Type="Element" Type="In" /> -->
                  <!--   </Effect> -->
                  <!-- </PresetMode> -->
                </Picture>
              </ElementList>
            </Document>
            """;
        var document = XDocument.Parse(xmlDocument);
        XElement root = document.Root!;
        foreach (XAttribute attribute in root.Attributes())
        {
            
        }
    }

    class Document
    {
        public List<IElement> ElementList { get; }
    }
    interface IElement { }
    class Picture
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Blip? Fill { get; set; }
        //public Effect? PresetMode { get; set; }
    }

    class Blip
    {
        public string? Source { get; set; }
    }

    //class Effect
    //{

    //}
}

