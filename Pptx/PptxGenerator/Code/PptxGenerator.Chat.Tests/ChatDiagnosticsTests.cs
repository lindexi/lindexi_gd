using System.IO;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ChatDiagnosticsTests
{
    [TestMethod]
    public void WritesFailureContextWithoutTheRawExceptionMessage()
    {
        var directory = Directory.CreateTempSubdirectory("PptxGenerator.Chat.Logs-");
        var path = ChatDiagnostics.StartFileLog(directory.FullName);
        try
        {
            Assert.IsNotNull(path);
            ChatDiagnostics.Failure("Probe", new IOException("sample-sensitive-content"), "ModelId", "demo");

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var log = reader.ReadToEnd();
            Assert.AreEqual((true, false), (log.Contains("[Chat][Probe] ModelId=demo ExceptionType=IOException"),
                log.Contains("sample-sensitive-content")));
        }
        finally
        {
            ChatDiagnostics.StopFileLog();
        }
    }
}
