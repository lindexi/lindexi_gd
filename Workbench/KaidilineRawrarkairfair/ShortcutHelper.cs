using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.System.Com;

namespace KaidilineRawrarkairfair;

internal static class ShortcutHelper
{
    /// <summary>
    /// 创建一个快捷方式
    /// </summary>
    /// <param name="lnkFilePath">快捷方式的完全限定路径。</param>
    /// <param name="workDir"></param>
    /// <param name="args">快捷方式启动程序时需要使用的参数。</param>
    /// <param name="targetPath"></param>
    public static unsafe void CreateShortcut(string lnkFilePath, string targetPath, string workDir, string args = "")
    {
        var shellLinkW = ShellLinkProvider.CreateShellLink();
        shellLinkW->SetPath(targetPath);
        shellLinkW->SetArguments(args);
        shellLinkW->SetWorkingDirectory(workDir);

        shellLinkW->QueryInterface(out IPersistFile* persistFile);
        //persistFile->SaveCompleted(lnkFilePath);
        persistFile->Save(lnkFilePath, false);
    }
}
