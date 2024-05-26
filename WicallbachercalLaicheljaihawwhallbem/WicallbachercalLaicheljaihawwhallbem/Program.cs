// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Security.Cryptography;
using Windows.Win32;
using Windows.Win32.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using WicallbachercalLaicheljaihawwhallbem;

var rootPath = Environment.CurrentDirectory;
if (args.Length > 0)
{
    rootPath = args[0];
}

var logFolder = Path.GetFullPath(Path.Join(rootPath, $"Log_{DateTime.Now:yyyy.MM.dd}"));
Directory.CreateDirectory(logFolder);

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy.MM.dd HH:mm:ss ";
    });
    builder.AddProvider(new LogFileProvider(new DirectoryInfo(logFolder)));
});

var logger = loggerFactory.CreateLogger("");

logger.LogInformation($"Start zip {rootPath} folder. LogFolder={logFolder}");

var sqliteFile = Path.Join(rootPath, "FileManger.db");
var sqliteFileWithoutExtension = Path.Join(rootPath, Path.GetFileNameWithoutExtension(sqliteFile.AsSpan()));

using (var fileStorageContext = new FileStorageContext(sqliteFile))
{
    fileStorageContext.Database.Migrate();
}

var destination = new byte[1024];
long saveSize = 0;

using (var fileStorageContext = new FileStorageContext(sqliteFile))
{
    foreach (var file in Directory.EnumerateFiles(rootPath, "*", enumerationOptions: new EnumerationOptions()
    {
        RecurseSubdirectories = true
    }))
    {
        if (file.StartsWith(sqliteFileWithoutExtension))
        {
            continue;
        }

        if (file.StartsWith(logFolder))
        {
            continue;
        }

        logger.LogInformation($"Start {file}");

        try
        {
            long fileLength;
            string sha1;
            await using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileLength = fileStream.Length;

                var length = await SHA1.HashDataAsync(fileStream, destination);
                sha1 = Convert.ToHexString(destination, 0, length);
            }

            var fileStorageModel = fileStorageContext.FileStorageModel.Find(sha1);
            if (fileStorageModel != null)
            {
                if (CreateHardLink(file, fileStorageModel.OriginFilePath))
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
                    OriginFilePath = file,
                    ReferenceCount = 1
                };
                fileStorageContext.FileStorageModel.Add(fileStorageModel);

                logger.LogInformation($"Not exists Record {file} SHA1={sha1}");
            }

            fileStorageContext.SaveChanges();
        }
        catch (Exception e)
        {
            logger.LogWarning($"Hard link fail {file} {e}");
        }
    }
}

Console.WriteLine($"Total save disk size: {UnitConverter.ConvertSize(saveSize, separators: " ")}");

static bool CreateHardLink(string file, string originFilePath)
{
    if (file == originFilePath)
    {
        return false;
    }

    File.Delete(file);

    var lpSecurityAttributes = new SECURITY_ATTRIBUTES()
    {
    };
    PInvoke.CreateHardLink(file, originFilePath, ref lpSecurityAttributes);

    return true;
}