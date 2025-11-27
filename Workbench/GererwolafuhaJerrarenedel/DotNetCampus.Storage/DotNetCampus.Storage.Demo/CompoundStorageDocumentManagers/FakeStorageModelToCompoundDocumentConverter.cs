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

    public override StorageModel ToStorageModel(CompoundStorageDocument document)
    {
        var fakeStorageModel = new FakeStorageModel()
        {
            Document = ReadRootSaveInfoProperty<TestDocumentSaveInfo>(document, "Document.xml"),
            Presentation = ReadRootSaveInfoProperty<PresentationSaveInfo>(document, "Presentation.xml"),
            SlideList = ReadRootSaveInfoPropertyList<SlideSaveInfo>(document, path =>
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
            }).ToList()
        };
        return fakeStorageModel;
    }

    public override CompoundStorageDocument ToCompoundDocument(StorageModel model)
    {
        var referencedManager = Manager.ReferencedManager;
        var storageItemList = new List<IStorageItem>();

        if (model is FakeStorageModel fakeStorageModel)
        {
            referencedManager.Reset();

            AddNode(fakeStorageModel.Document, "Document.xml");
            AddNode(fakeStorageModel.Presentation, "Presentation.xml");

            if (fakeStorageModel.SlideList is { } slideList)
            {
                for (var i = 0; i < slideList.Count; i++)
                {
                    var slideSaveInfo = slideList[i];
                    var relativePath = $@"Slides\Slide{i + 1}.xml";
                    AddNode(slideSaveInfo, relativePath);
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

        void AddNode<T>(T? value, StorageFileRelativePath relativePath)
        {
            var storageNodeItem = ToStorageNodeItem(value, relativePath);
            if (storageNodeItem != null)
            {
                storageItemList.Add(storageNodeItem);
            }
        }
    }
}