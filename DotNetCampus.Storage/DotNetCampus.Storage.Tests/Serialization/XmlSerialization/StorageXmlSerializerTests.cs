using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DotNetCampus.Storage.Serialization.XmlSerialization;
using DotNetCampus.Storage.Tests.Assets;

namespace DotNetCampus.Storage.Tests.Serialization.XmlSerialization;

public partial class StorageXmlSerializerTests
{
    [TestMethod()]
    public async Task TestDeserializeAsync()
    {
        var testFile = TestFileProvider.GetTestFile("Slide_1_S4509.7082muxhueq.wc3.xml");

        var storageXmlSerializer = new StorageXmlSerializer();
        var storageNode = await storageXmlSerializer.DeserializeAsync(testFile);

        var outputFile = new FileInfo(Path.GetTempFileName());
        await storageXmlSerializer.SerializeAsync(storageNode, outputFile);

        // 对比两个文件是否相同
        var sourceText = await File.ReadAllTextAsync(testFile.FullName);
        var outputText = await File.ReadAllTextAsync(outputFile.FullName);

        sourceText = sourceText.Replace("\r\n", "\n")
            .Replace("<Text></Text>", "<Text />");
        outputText = outputText.Replace("\r\n", "\n");

        Assert.AreEqual(sourceText, outputText);

        outputFile.Delete();
    }
}

[TestClass()]
public partial class StorageXmlSerializerTests
{
    [TestMethod()]
    public async Task ParseTest()
    {
        // 这里的 slide1.xml 是一份超级大的文档
        // 可见全部都加载成了对象，这是很大的损耗
        var testFile = @"C:\lindexi\slide1.xml";
        if (!File.Exists(testFile))
        {
            return;
        }

        using var fileStream = File.OpenRead(testFile);
        var document = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);

        //XmlReader xmlReader = XmlReader.Create(fileStream);
        // XML 文档内容：
        /*
        <Document Type="Element">
        </Document>
         */
        var storageXmlSerializer = new StorageXmlSerializer();
        var storageNode = storageXmlSerializer.Deserialize(document);
        var outputFile = new FileInfo(Path.GetTempFileName());
        await storageXmlSerializer.SerializeAsync(storageNode, outputFile);
        outputFile.Delete();
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
        public List<IElement> ElementList { get; } = null!;
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

