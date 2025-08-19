// See https://aka.ms/new-console-template for more information

using RallcewawhahereLunucaje;

var testLogFolder = $"Log_{Path.GetRandomFileName()}";

var dateTime = DateTime.Now;

for (int i = 0; i < 10; i++)
{
    dateTime = dateTime.AddDays(-1);
    var logFolderName = IndependentProcessLogFolderManager.GetLogFolderName(dateTime, i);
    var directoryInfo = Directory.CreateDirectory(Path.Join(testLogFolder, logFolderName));
    directoryInfo.CreationTime = dateTime;
}

var regularCleaningLogTask = new RegularCleaningLogTask(testLogFolder);

var t1 = regularCleaningLogTask.Run();
_ = regularCleaningLogTask.Run();

await t1;
Console.ReadLine();
