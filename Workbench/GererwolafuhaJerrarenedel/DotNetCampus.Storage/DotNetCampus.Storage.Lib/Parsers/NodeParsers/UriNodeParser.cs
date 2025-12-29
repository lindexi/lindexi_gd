using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.Standard;
using DotNetCampus.Storage.StorageNodes;

using DotNetCampus.Storage.Documents.StorageDocuments;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Parsers.NodeParsers;

internal class UriNodeParser<TStorageUri> : AsyncPostNodeParser<TStorageUri>
    where TStorageUri : StorageUri
{
    public override string? TargetStorageName => null;

    protected internal override TStorageUri ParseCore(StorageNode node, in ParseNodeContext context)
    {
        var value = node.Value.ToText();

        var storageUri = StorageUri.Create(value);
        var referencedManager = context.DocumentManager.ReferencedManager;

        if (storageUri is null)
        {
            ReferenceInfo? referenceInfo = referencedManager.GetReferenceInfo(new StorageReferenceId(value));

            if (referenceInfo != null)
            {
                storageUri = ToFileUri(referenceInfo);
            }
            else
            {
                storageUri = new FileUri(value);
            }
        }

        var type = typeof(TStorageUri);
        if (type == typeof(IdUri))
        {
            return ToResult(storageUri);
        }
        else
        {
            // FileUri 或 StorageUri 的情况，都返回 FileUri 内容
            if (storageUri is IdUri idUri)
            {
                ReferenceInfo? referenceInfo = referencedManager.GetReferenceInfo(new StorageReferenceId(idUri.Value));
                if (referenceInfo != null)
                {
                    var fileUri = ToFileUri(referenceInfo);
                    return ToResult(fileUri);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return ToResult(storageUri);
        }

        FileUri ToFileUri(ReferenceInfo referenceInfo)
        {
            var readOnlyStorageFileInfo = referencedManager.StorageFileManager.GetFile(referenceInfo.FilePath);
            if (readOnlyStorageFileInfo is null)
            {
                throw new StorageReferenceNotFoundException(referenceInfo.ReferenceId, referencedManager);
            }

            if (readOnlyStorageFileInfo is not LocalStorageFileInfo localStorageFileInfo)
            {
                // 预期不会进入此
                localStorageFileInfo = referencedManager.StorageFileManager.ToLocalStorageFileInfoAsync(readOnlyStorageFileInfo).Result;
            }

            return localStorageFileInfo.FileInfo;
        }

        static TStorageUri ToResult(StorageUri uri)
        {
            if (uri is TStorageUri result)
            {
                return result;
            }

            throw new InvalidOperationException();
        }
    }

    protected internal override StorageNode DeparseCore(TStorageUri obj, in DeparseNodeContext context)
    {
        var name = context.NodeName;
        return new StorageNode()
        {
            Name = name,
            Value = obj.Encode(),
        };
    }

    protected override async Task<StorageNode> PostDeparseCoreAsync(TStorageUri obj, StorageNode oldStorageNode, DeparseNodeContext context)
    {
        var referencedManager = context.DocumentManager.ReferencedManager;
        StorageTextSpan value;

        if (obj is FileUri fileUri)
        {
            var fileInfo = new FileInfo(fileUri.Value);
            var referenceInfo = referencedManager.AddLocalFile(fileInfo);
            var storageReferenceId = referenceInfo.ReferenceId;

            var idUri = new IdUri(storageReferenceId.ReferenceId);
            value = idUri.Encode();
        }
        else if (obj is IdUri idUri)
        {
            value = idUri.Encode();
        }
        else
        {
            throw new InvalidOperationException();
        }


        return oldStorageNode;
    }
}