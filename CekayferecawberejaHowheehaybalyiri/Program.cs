// See https://aka.ms/new-console-template for more information

using Renci.SshNet;

var sshClient = new SshClient("172.20.114.99","lin","123");
sshClient.Connect();

var execute = sshClient.RunCommand("ls").Execute();
Console.WriteLine(execute);

