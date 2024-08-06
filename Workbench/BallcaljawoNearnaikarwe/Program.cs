// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

var startInfo = new ProcessStartInfo("cmd")
{
    WorkingDirectory = Environment.CurrentDirectory,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    RedirectStandardInput = true,

    //Arguments = "ssh -o ServerAliveInterval=600 uos@172.20.113.26",
};

// 虚拟机： 172.20.113.26
// 账号: uos
// 密码: 123

var process = new Process()
{
    StartInfo = startInfo
};
process.Start();

process.OutputDataReceived += Process_OutputDataReceived;
var standardOutput = process.StandardOutput;
var standardError = process.StandardError;
var standardInput = process.StandardInput;

var line = standardOutput.ReadLine();
Console.WriteLine($"[StandardOutput] {line}");


standardInput.WriteLine("ssh -o ServerAliveInterval=600 uos@172.20.113.26");

line = standardOutput.ReadLine();
Console.WriteLine($"[StandardOutput] {line}");

await Task.Delay(3000);

standardInput.WriteLine("123");
standardInput.Flush();

Console.SetIn(standardOutput);
Console.SetOut(standardInput);

while (true)
{
    Console.Read();
}

void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
{
    Console.WriteLine($"[OutputDataReceived] {e.Data}");
}