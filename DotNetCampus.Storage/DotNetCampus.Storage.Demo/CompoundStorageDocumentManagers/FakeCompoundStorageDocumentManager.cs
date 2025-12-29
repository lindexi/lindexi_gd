using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Parsers;
using DotNetCampus.Storage.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Demo.CompoundStorageDocumentManagers;

class FakeCompoundStorageDocumentManager : CompoundStorageDocumentManager
{
    public FakeCompoundStorageDocumentManager()
    {
        StorageModelToCompoundDocumentConverter = new FakeStorageModelToCompoundDocumentConverter(this);
        CompoundStorageDocumentSerializer = new FakeCompoundStorageDocumentSerializer(this);
        var parserManager = new StorageNodeParserManager();
        StorageNodeParserManagerCollection.RegisterSaveInfoNodeParser(parserManager);
        ParserManager = parserManager;
    }

    public override StorageNodeParserManager ParserManager { get; }

    public override IStorageModelToCompoundDocumentConverter StorageModelToCompoundDocumentConverter { get; }
    public override ICompoundStorageDocumentSerializer CompoundStorageDocumentSerializer { get; }
}