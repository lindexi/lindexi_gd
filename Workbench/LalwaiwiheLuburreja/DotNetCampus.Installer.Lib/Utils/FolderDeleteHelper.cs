namespace DotNetCampus.Installer.Lib.Utils;

public class FolderDeleteHelper
{
    /// <summary>
    /// 递归删除文件夹，包括其中的所有文件和文件夹。
    /// </summary>
    /// <remarks>
    /// 这是不使用系统自身的递归删除方法`Directory.Delete(dirPath, true)`是因为此方法有坑。
    /// 可能导致文件删除失败之后再无访问权限。
    /// </remarks>
    /// <param name="directoryPath"></param>
    /// <param name="throwException">是否把异常抛出由业务方处理</param>
    /// <param name="onlyDeleteContent">删除文件夹中所有的文件和文件夹，但不删除文件夹本身</param>
    public static void DeleteFolder(string directoryPath, bool throwException = false,bool onlyDeleteContent=false)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        var directory = directoryPath;
        var subDirectoryStack = new Stack<string>();
        subDirectoryStack.Push(directory);

        var exceptionList = new List<Exception>();

        var folderList = new List<string>();

        // 尽可能地删除目录中的文件。

        // 如果出现异常也不需要记录
        while (subDirectoryStack.Any())
        {
            var dir = subDirectoryStack.Pop();
            folderList.Add(dir);

            try
            {
                var files = Directory.GetFiles(dir);
                foreach (var file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        exceptionList.Add(e);
                    }
                }
            }
            catch (Exception e)
            {
                exceptionList.Add(e);
            }

            try
            {
                var subDirectories = Directory.GetDirectories(dir);
                foreach (var subDirectory in subDirectories)
                {
                    subDirectoryStack.Push(subDirectory);
                }
            }
            catch (Exception e)
            {
                exceptionList.Add(e);
            }
        }

        // 删除目录结构。
        try
        {
            // 从后向前删除目录，避免删除父目录时子目录还存在导致异常
            for (var i = folderList.Count - 1; i >= 0; i--)
            {
                try
                {
                    Directory.Delete(folderList[i], true);
                }
                catch (Exception e)
                {
                    exceptionList.Add(e);
                }
            }

            if (!onlyDeleteContent && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
        catch (Exception e)
        {
            exceptionList.Add(e);
        }

        if (exceptionList.Any())
        {
            var exception = new AggregateException(exceptionList);
            if (throwException)
            {
                throw exception;
            }
            else
            {
                Console.WriteLine($"Error when DeleteFolder {directoryPath}", exception);
            }
        }
    }
}
