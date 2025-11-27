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

        var fileList = new List<IReadOnlyStorageFileInfo>(zipArchive.Entries.Count);

        foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
        {
            var opcStorageFileInfo = new OpcStorageFileInfo(zipArchiveEntry);
            fileList.Add(opcStorageFileInfo);
        }

        var fileProvider = new SerializationStorageFileProvider(fileList);

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
/// 一个干净的存放文件信息的管理器，只存放被添加进去的文件信息
/// </summary>
internal class CleanStorageFileManager : IReadOnlyStorageFileManager
{
    public CleanStorageFileManager(IStorageFileManager backStorageFileManager)
    {
        BackStorageFileManager = backStorageFileManager;
    }

    public IStorageFileInfo CreateFile(StorageFileRelativePath relativePath)
    {
        var fileInfo = BackStorageFileManager.CreateFile(relativePath);
        AddFile(fileInfo);
        return fileInfo;
    }

    private List<IReadOnlyStorageFileInfo> FileList { get; } = [];

    /// <summary>
    /// 后备的文件存储管理器。实际的文件存储管理器
    /// </summary>
    public IStorageFileManager BackStorageFileManager { get; }

    IReadOnlyCollection<IReadOnlyStorageFileInfo> IReadOnlyStorageFileManager.FileList => FileList;

    public void AddFile(IReadOnlyStorageFileInfo fileInfo)
    {
        FileList.Add(fileInfo);
    }
}

/// <summary>
/// 存放序列化之后的文件信息
/// </summary>
/// <param name="FileList"></param>
public record SerializationStorageFileProvider(IReadOnlyCollection<IReadOnlyStorageFileInfo> FileList) : IReadOnlyStorageFileManager
{
    // 设计上应该是从 IStorageFileManager 源到复合存储文档之间的转换
    // 这个过程需要关注的是哪个文件到哪个文件之间的映射关系
    // 在 CompoundStorageDocumentManager 将一批 XML 相关的转换器放在一起。同时开放用户的配置逻辑
    // 如此可以搞定从 CompoundStorageDocumentManager 转换序列化的本地文件到存储文档的过程
    // 以及从存储文档转换到内存模型的过程。存储文档本身需要注入文档的文件到对应哪个存储模型的关系
}