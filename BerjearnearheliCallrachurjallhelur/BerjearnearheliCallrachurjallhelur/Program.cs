using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BerjearnearheliCallrachurjallhelur
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = @"e:\上传\坚果云\回收站\网络课程\Enbx\";
            var pptFolder = @"e:\上传\坚果云\回收站\网络课程\PPTX\";

            var outputFolder = @"f:\temp\分配的课件任务\";

            var enbxFileList = Directory.GetFiles(folder, "*.enbx", SearchOption.AllDirectories);

            foreach (var file in Directory.GetFiles(pptFolder).Select(temp => new FileInfo(temp)))
            {
                var fileName = Path.GetFileNameWithoutExtension(file.FullName);

                var output = Path.Combine(outputFolder, fileName);
                Directory.CreateDirectory(output);

                var matchFileList = enbxFileList.Where(temp=>temp.Contains(fileName)).ToList();
                file.CopyTo(Path.Combine(output, file.Name));
                foreach (var enbxFile in matchFileList)
                {
                    var newEnbxName = fileName + ".enbx";
                    var newEnbxFile = Path.Combine(output, newEnbxName);

                    while (File.Exists(newEnbxFile))
                    {
                        newEnbxName = "修改版 " + newEnbxName;
                        newEnbxFile = Path.Combine(output, newEnbxName);
                    }

                    File.Copy(enbxFile,newEnbxFile);
                }
            }

            Console.Read();
        }
    }
}