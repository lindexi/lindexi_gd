using System.Text.Json;

namespace XiaoXiIme.SandboxRunner;

internal static class G02EvidenceRecovery
{
    internal static int Run()
    {
        var output = Path.Combine(AppContext.BaseDirectory, "recovery");
        Directory.CreateDirectory(output);
        var tasks = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
        var records = new List<object>();
        foreach (var task in Directory.EnumerateDirectories(tasks))
        {
            var evidence = Path.Combine(task, "evidence");
            var execution = Path.Combine(evidence, "execution.json");
            if (!File.Exists(execution))
            {
                continue;
            }

            var info = new FileInfo(execution);
            if (info.Length > 2 * 1024 * 1024)
            {
                records.Add(new { task, status = "execution-too-large", info.Length });
                continue;
            }

            var destination = Path.Combine(output, Path.GetFileName(task));
            Directory.CreateDirectory(destination);
            foreach (var name in new[]
                     {
                         "execution.json", "report.json", "failure.txt", "stdout.txt", "stderr.txt", "host.stdout.txt",
                         "host.stderr.txt", "target.stdout.txt", "target.stderr.txt"
                     })
            {
                var source = Path.Combine(evidence, name);
                if (!File.Exists(source))
                {
                    continue;
                }

                var file = new FileInfo(source);
                if (file.Length <= 2 * 1024 * 1024)
                {
                    File.Copy(source, Path.Combine(destination, name), true);
                }

                records.Add(new { task, name, file.Length, copied = file.Length <= 2 * 1024 * 1024 });
            }
        }

        File.WriteAllText
        (
            Path.Combine(output, "recovery.json"),
            JsonSerializer.Serialize(new { tasks, records }, new JsonSerializerOptions { WriteIndented = true })
        );
        return 0;
    }
}