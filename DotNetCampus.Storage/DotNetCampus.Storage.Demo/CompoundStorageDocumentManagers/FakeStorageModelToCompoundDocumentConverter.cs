using System.Text.RegularExpressions;

using DotNetCampus.Storage.Demo.SaveInfos;
using DotNetCampus.Storage.Documents.Converters;
using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.Documents.StorageDocuments.StorageItems;
using DotNetCampus.Storage.Documents.StorageModels;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Demo.CompoundStorageDocumentManagers;

class FakeStorageModelToCompoundDocumentConverter : StorageModelToCompoundDocumentConverter
{
    public FakeStorageModelToCompoundDocumentConverter(CompoundStorageDocumentManager manager) : base(manager)
    {
    }

    public override async Task<StorageModel> ToStorageModel(CompoundStorageDocument document)
    {
        var fakeStorageModel = new FakeStorageModel()
        {
            Document = await ReadRootSaveInfoPropertyAsync<TestDocumentSaveInfo>(document, "Document.xml"),
            Presentation = await ReadRootSaveInfoPropertyAsync<PresentationSaveInfo>(document, "Presentation.xml"),
            SlideList = await ReadRootSaveInfoPropertyListAsync<SlideSaveInfo>(document, path =>
            {
                var relativePath = path.RelativePath;
                if (relativePath.Contains('\\') || relativePath.Contains('/'))
                {
                    if (Path.GetDirectoryName(relativePath.AsSpan()) is "Slides")
                    {
                        var fileName = Path.GetFileName(relativePath.AsSpan());
                        return Regex.IsMatch(fileName, @"Slide\d+\.xml");
                    }
                }

                return false;
            })
        };
        return fakeStorageModel;
    }

    public override async Task<CompoundStorageDocument> ToCompoundDocument(StorageModel model)
    {
        var referencedManager = Manager.ReferencedManager;
        var storageItemList = new List<IStorageItem>();

        if (model is FakeStorageModel fakeStorageModel)
        {
            referencedManager.Reset();

            await AddNodeAsync(fakeStorageModel.Document, "Document.xml");
            await AddNodeAsync(fakeStorageModel.Presentation, "Presentation.xml");

            if (fakeStorageModel.SlideList is { } slideList)
            {
                for (var i = 0; i < slideList.Count; i++)
                {
                    var slideSaveInfo = slideList[i];
                    var relativePath = $"Slides/Slide{i + 1}.xml";
                    await AddNodeAsync(slideSaveInfo, relativePath);
                }
            }

            foreach (var referenceInfo in referencedManager.References)
            {
                var storageResourceItem = new StorageResourceItem()
                {
                    ResourceId = referenceInfo.ReferenceId,
                    RelativePath = referenceInfo.FilePath,
                };
                storageItemList.Add(storageResourceItem);
            }
        }

        var compoundStorageDocument = new CompoundStorageDocument(storageItemList, referencedManager);
        return compoundStorageDocument;

        async Task AddNodeAsync<T>(T? value, StorageFileRelativePath relativePath)
        {
            if (value is null)
            {
                return;
            }

            var storageNode = await Manager.DeparseToStorageNodeAsync(value);
            var storageNodeItem = new StorageNodeItem()
            {
                RootStorageNode = storageNode,
                RelativePath = relativePath
            };
            storageItemList.Add(storageNodeItem);
        }
    }
}