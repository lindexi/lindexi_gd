using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

if (!OperatingSystem.IsWindows())
{
    return;
}

using (var pipe = NamedPipeServerStreamAcl.Create("FooPipe", PipeDirection.InOut, -1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, null, HandleInheritability.None, PipeAccessRights.ChangePermissions))
{
    PipeSecurity ps = pipe.GetAccessControl();
    PipeAccessRule clientRule = new PipeAccessRule(
        new SecurityIdentifier("S-1-15-2-1"), // All application packages
        PipeAccessRights.ReadWrite,
        AccessControlType.Allow);
    PipeAccessRule ownerRule = new PipeAccessRule(
        WindowsIdentity.GetCurrent().Owner!,
        PipeAccessRights.FullControl,
        AccessControlType.Allow);
    ps.AddAccessRule(clientRule);
    ps.AddAccessRule(ownerRule);
    pipe.SetAccessControl(ps);
    pipe.WaitForConnection();
    using (var streamReader = new StreamReader(pipe, Encoding.UTF8))
    {
        for (int i = 0; i < int.MaxValue; i++)
        {
            string? message = streamReader.ReadLine();

            Console.WriteLine($"收到消息： {message}");

            var streamWriter = new StreamWriter(pipe, Encoding.UTF8);
            streamWriter.WriteLine($"[{i}] 服务端已收到消息");
            streamWriter.Flush();
        }
    }
}