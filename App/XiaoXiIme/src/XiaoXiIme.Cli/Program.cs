using XiaoXiIme.Cli;

return CliApplication.Run(
    args,
    Console.Out,
    Console.Error,
    static () => new WindowsImeInstaller());
