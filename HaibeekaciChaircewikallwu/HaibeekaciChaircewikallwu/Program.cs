// See https://aka.ms/new-console-template for more information
var file = @"f:\temp\每个 commit 构建日志 减少磁盘空间\Log.txt";
var recordCount = 0;
var notRecordCount = 0;
await foreach (var line in File.ReadLinesAsync(file))
{
    if (line.Contains("Not exists Record"))
    {
        notRecordCount++;
    }
    else if (line.Contains("Exists Record SHA1"))
    {
        recordCount++;
    }
}

Console.WriteLine($"记录数量：{recordCount} 没记录数量：{notRecordCount};比例：{(recordCount * 1.0 / (recordCount + notRecordCount)):0.00}");