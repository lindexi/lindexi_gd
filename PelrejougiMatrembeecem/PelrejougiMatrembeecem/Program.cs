using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PelrejougiMatrembeecem
{
    class Program
    {
        static void Main(string[] args)
        {
            object shDesktop = "Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = (string) shell.SpecialFolders.Item(ref shDesktop) + @"\Recycle Bin.lnk";
            IWshShortcut shortcut = (IWshShortcut) shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "New shortcut for Recycle Bin";
            shortcut.Hotkey = "Ctrl+Shift+N";
            shortcut.IconLocation = @"C:\WINDOWS\System32\imageres.dll";
            shortcut.TargetPath = "::{645ff040-5081-101b-9f08-00aa002f954e}";
            shortcut.Save();
        }
    }
}