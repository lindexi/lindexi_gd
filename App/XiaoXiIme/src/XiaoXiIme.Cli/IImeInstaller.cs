namespace XiaoXiIme.Cli;

internal interface IImeInstaller
{
    ImeInstallationResult Install(string imeFilePath, string displayName);

    ImeInstallationResult InstallPair(string x64ImeFilePath, string x86ImeFilePath, string displayName);

    ImeUninstallationResult UninstallExisting(string displayName, string imeFileName);
}