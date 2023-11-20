using System.IO;
using System.Security.Cryptography;
using Windows.Win32;
using Windows.Win32.Security;
using Microsoft.Extensions.Logging;

namespace UsingHardLinkToZipNtfsDiskSize;

public class UsingHardLinkToZipNtfsDiskSizeProvider
{
    /// <summary>
    /// 开始将 <paramref name="workFolder"/> 文件夹里面重复的文件使用硬连接压缩磁盘空间
    /// </summary>
    /// <param name="workFolder"></param>
    /// <param name="fileStorageContext"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public async Task Start(DirectoryInfo workFolder, FileStorageContext fileStorageContext, ILogger logger)
    {
        var destination = new byte[1024];
        long saveSize = 0;
        foreach (var file in workFolder.EnumerateFiles("*", enumerationOptions: new EnumerationOptions()
                 {
                     RecurseSubdirectories = true
                 }))
        {
            logger.LogInformation($"Start {file}");

            try
            {
                long fileLength;
                string sha1;
                await using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fileLength = fileStream.Length;

                    var length = await SHA1.HashDataAsync(fileStream, destination);
                    sha1 = Convert.ToHexString(destination, 0, length);
                }

                var fileStorageModel = await fileStorageContext.FileStorageModel.FindAsync(sha1);
                if (fileStorageModel != null)
                {
                    if (CreateHardLink(file.FullName, fileStorageModel.OriginFilePath))
                    {
                        // 省的空间
                        saveSize += fileLength;
                        logger.LogInformation(
                            $"Exists Record SHA1={sha1} {file} SaveSize：{UnitConverter.ConvertSize(saveSize, separators: " ")}");

                        fileStorageModel.ReferenceCount++;
                        fileStorageContext.FileStorageModel.Update(fileStorageModel);
                    }
                }
                else
                {
                    fileStorageModel = new FileStorageModel()
                    {
                        FileLength = fileLength,
                        FileSha1Hash = sha1,
                        OriginFilePath = file.FullName,
                        ReferenceCount = 1
                    };
                    fileStorageContext.FileStorageModel.Add(fileStorageModel);

                    logger.LogInformation($"Not exists Record {file} SHA1={sha1}");
                }

                await fileStorageContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogWarning($"Hard link fail {file} {e}");
            }
        }

        logger.LogInformation($"Total save disk size: {UnitConverter.ConvertSize(saveSize, separators: " ")}");
    }

    private static bool CreateHardLink(string file, string originFilePath)
    {
        if (file == originFilePath)
        {
            return false;
        }
        File.Delete(file);

        var lpSecurityAttributes = new SECURITY_ATTRIBUTES();
        PInvoke.CreateHardLink(file, originFilePath, ref lpSecurityAttributes);

        return true;
    }
}