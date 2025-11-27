using DotNetCampus.Storage.Documents.StorageDocuments;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Serialization;

/// <summary>
/// 使用 Open Packaging Conventions(OPC) 格式的序列化器
/// </summary>
/// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/opc/open-packaging-conventions-overview
/// > The ECMA-376 OpenXML, 1st Edition, Part 2: Open Packaging Conventions (OPC) can be more easily understood through an analogy with real world filing systems. 
public class OpcSerializer
{
    public OpcSerializer(CompoundStorageDocumentManager manager)
    {
        _manager = manager;
    }

    private readonly CompoundStorageDocumentManager _manager;

    public async Task<CompoundStorageDocument> ReadFromOpcFileAsync(FileInfo opcFile)
    {
        await using var fileStream = opcFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

        var fileList = new List<OpcStorageFileInfo>(zipArchive.Entries.Count);

        foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
        {
            var opcStorageFileInfo = new OpcStorageFileInfo(zipArchiveEntry);
            fileList.Add(opcStorageFileInfo);
        }

        var fileProvider = new OpcVirtualStorageFileManager(fileList);

        var compoundStorageDocument = await _manager.CompoundStorageDocumentSerializer.ToCompoundStorageDocument(fileProvider);
        return compoundStorageDocument;
    }

    public async Task SaveToOpcFileAsync(CompoundStorageDocument document, FileInfo opcOutputFile)
    {
        await using var fileStream = opcOutputFile.Create();
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true);

        var storageFileManager = await _manager.CompoundStorageDocumentSerializer.ToStorageFileManager(document);
        foreach (IReadOnlyStorageFileInfo fileInfo in storageFileManager.FileList)
        {
            var zipArchiveEntry = zipArchive.CreateEntry(fileInfo.RelativePath.RelativePath, CompressionLevel.Optimal);
            await using var zipStream = zipArchiveEntry.Open();
            await using var stream = fileInfo.OpenRead();
            await stream.CopyToAsync(zipStream);
        }
    }
}

/// <summary>
/// 用于 Opc 格式里面，虚拟的文件存放器。里面存放的是 <see cref="OpcStorageFileInfo"/> 类型
/// </summary>
file record OpcVirtualStorageFileManager : IReadOnlyStorageFileManager
{
    public OpcVirtualStorageFileManager(IReadOnlyList<OpcStorageFileInfo> opcFileList)
    {
        OpcFileList = opcFileList;
    }

    public IReadOnlyList<OpcStorageFileInfo> OpcFileList { get; }

    public IReadOnlyCollection<IReadOnlyStorageFileInfo> FileList => OpcFileList;

    // 设计上应该是从 IStorageFileManager 源到复合存储文档之间的转换
    // 这个过程需要关注的是哪个文件到哪个文件之间的映射关系
    // 在 CompoundStorageDocumentManager 将一批 XML 相关的转换器放在一起。同时开放用户的配置逻辑
    // 如此可以搞定从 CompoundStorageDocumentManager 转换序列化的本地文件到存储文档的过程
    // 以及从存储文档转换到内存模型的过程。存储文档本身需要注入文档的文件到对应哪个存储模型的关系
}