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
    }
}