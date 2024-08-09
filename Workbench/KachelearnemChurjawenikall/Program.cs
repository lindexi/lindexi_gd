using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KachelearnemChurjawenikall;
internal class Program
{
    [SkipLocalsInit]
    static void Main(string[] args)
    {
        var sourceFile = @"F:\temp\1";
        var targetFile = @"F:\temp\2";

        if (!File.Exists(sourceFile))
        {
            var buffer = new byte[1024 * 1024];
            for (int i = 0; i < 1000; i++)
            {
                Random.Shared.NextBytes(buffer);
                File.AppendAllBytes(sourceFile, buffer);
            }
        }

        string[] sourceFiles = [sourceFile];
        string[] targetFiles = [targetFile];

        SHFILEOPSTRUCT pm = new SHFILEOPSTRUCT();
        pm.wFunc = wFunc.FO_COPY;

        //设置对话框标题，在win7中无效
        pm.lpszProgressTitle = "复制文件";
        pm.pFrom = string.Join(FILE_SPLITER, sourceFiles) + $"{FILE_SPLITER}{FILE_SPLITER}";
        pm.pTo = string.Join(FILE_SPLITER, targetFiles) + $"{FILE_SPLITER}{FILE_SPLITER}";

        pm.fFlags = FILEOP_FLAGS.FOF_NOCONFIRMATION | FILEOP_FLAGS.FOF_MULTIDESTFILES | FILEOP_FLAGS.FOF_ALLOWUNDO;

        SHFileOperation(pm);
    }

    /// <summary>

    /// 映射API方法

    /// </summary>

    /// <param name="lpFileOp"></param>

    /// <returns></returns>

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]

    private static extern int SHFileOperation(SHFILEOPSTRUCT lpFileOp);



    /// <summary>

    /// 多个文件路径的分隔符

    /// </summary>

    private const string FILE_SPLITER = "\0";



    /// <summary>

    /// Shell文件操作数据类型

    /// </summary>

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

    private class SHFILEOPSTRUCT

    {

        public IntPtr hwnd;

        /// <summary>

        /// 设置操作方式

        /// </summary>

        public wFunc wFunc;

        /// <summary>

        /// 源文件路径

        /// </summary>

        public string pFrom;

        /// <summary>

        /// 目标文件路径

        /// </summary>

        public string pTo;

        /// <summary>

        /// 允许恢复

        /// </summary>

        public FILEOP_FLAGS fFlags;

        /// <summary>

        /// 监测有无中止

        /// </summary>

        public bool fAnyOperationsAborted;

        public IntPtr hNameMappings;

        /// <summary>

        /// 设置标题

        /// </summary>

        public string lpszProgressTitle;

    }



    /// <summary>

    /// 文件操作方式

    /// </summary>

    private enum wFunc

    {

        /// <summary>

        /// 移动

        /// </summary>

        FO_MOVE = 0x0001,

        /// <summary>

        /// 复制

        /// </summary>

        FO_COPY = 0x0002,

        /// <summary>

        /// 删除

        /// </summary>

        FO_DELETE = 0x0003,

        /// <summary>

        /// 重命名

        /// </summary>

        FO_RENAME = 0x0004

    }



    /// <summary>

    /// fFlags枚举值，

    /// 参见：http://msdn.microsoft.com/zh-cn/library/bb759795(v=vs.85).aspx

    /// </summary>

    private enum FILEOP_FLAGS

    {



        ///<summary>

        ///pTo 指定了多个目标文件，而不是单个目录

        ///The pTo member specifies multiple destination files (one for each source file) rather than one directory where all source files are to be deposited.

        ///</summary>

        FOF_MULTIDESTFILES = 0x1,

        ///<summary>

        ///不再使用

        ///Not currently used.

        ///</summary>

        FOF_CONFIRMMOUSE = 0x2,

        ///<summary>

        ///不显示一个进度对话框

        ///Do not display a progress dialog box.

        ///</summary>

        FOF_SILENT = 0x4,

        ///<summary>

        ///碰到有抵触的名字时，自动分配前缀

        ///Give the file being operated on a new name in a move, copy, or rename operation if a file with the target name already exists.

        ///</summary>

        FOF_RENAMEONCOLLISION = 0x8,

        ///<summary>

        ///不对用户显示提示

        ///Respond with "Yes to All" for any dialog box that is displayed.

        ///</summary>

        FOF_NOCONFIRMATION = 0x10,

        ///<summary>

        ///填充 hNameMappings 字段，必须使用 SHFreeNameMappings 释放

        ///If FOF_RENAMEONCOLLISION is specified and any files were renamed, assign a name mapping object containing their old and new names to the hNameMappings member.

        ///</summary>

        FOF_WANTMAPPINGHANDLE = 0x20,

        ///<summary>

        ///允许撤销

        ///Preserve Undo information, if possible. If pFrom does not contain fully qualified path and file names, this flag is ignored.

        ///</summary>

        FOF_ALLOWUNDO = 0x40,

        ///<summary>

        ///使用 *.* 时, 只对文件操作

        ///Perform the operation on files only if a wildcard file name (*.*) is specified.

        ///</summary>

        FOF_FILESONLY = 0x80,

        ///<summary>

        ///简单进度条，意味着不显示文件名。

        ///Display a progress dialog box but do not show the file names.

        ///</summary>

        FOF_SIMPLEPROGRESS = 0x100,

        ///<summary>

        ///建新目录时不需要用户确定

        ///Do not confirm the creation of a new directory if the operation requires one to be created.

        ///</summary>

        FOF_NOCONFIRMMKDIR = 0x200,

        ///<summary>

        ///不显示出错用户界面

        ///Do not display a user interface if an error occurs.

        ///</summary>

        FOF_NOERRORUI = 0x400,

        ///<summary>

        /// 不复制 NT 文件的安全属性

        ///Do not copy the security attributes of the file.

        ///</summary>

        FOF_NOCOPYSECURITYATTRIBS = 0x800,

        ///<summary>

        /// 不递归目录

        ///Only operate in the local directory. Don't operate recursively into subdirectories.

        ///</summary>

        FOF_NORECURSION = 0x1000,

        ///<summary>

        ///Do not move connected files as a group. Only move the specified files.

        ///</summary>

        FOF_NO_CONNECTED_ELEMENTS = 0x2000,

        ///<summary>

        ///Send a warning if a file is being destroyed during a delete operation rather than recycled. This flag partially overrides FOF_NOCONFIRMATION.

        ///</summary>

        FOF_WANTNUKEWARNING = 0x4000,

        ///<summary>

        ///Treat reparse points as objects, not containers.

        ///</summary>

        FOF_NORECURSEREPARSE = 0x8000,

    }
}
