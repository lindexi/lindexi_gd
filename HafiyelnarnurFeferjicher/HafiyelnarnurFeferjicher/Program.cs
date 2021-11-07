using System;
using System.IO;
using Microsoft.Office.Interop.Word;

namespace HafiyelnarnurFeferjicher
{
    class Program
    {
        static void Main(string[] args)
        {
            var applicationClass = new ApplicationClass();
            //applicationClass.Visible = false; 默认值就是 false 值
            var folder = Environment.CurrentDirectory;
            string fileName = "1.docx";
            fileName = Path.Combine(folder, fileName);
            fileName = Path.GetFullPath(fileName);

            // 截图使用只读方式打开，这里传入的要求是绝对路径
            Document document = applicationClass.Documents.Open(fileName, ReadOnly: true);

            var count = 0;

            foreach (Window documentWindow in document.Windows)
            {
                var documentWindowPanes = documentWindow.Panes;
                foreach (Pane documentWindowPane in documentWindowPanes)
                {
                    var pagesCount = documentWindowPane.Pages.Count;
                    for (int i = 0; i < pagesCount; i++)
                    {
                        Page page = documentWindowPane.Pages[i + 1];
                        Console.WriteLine($"{page.Width};{page.Height}");
                        count++;
                        var file = Path.Combine(folder, $"{count}.png");

                        var bits = page.EnhMetaFileBits;
                        using (var ms = new MemoryStream((byte[])(bits)))
                        {
                            var image = System.Drawing.Image.FromStream(ms);
                            image.Save(file);
                        }
                        //page.SaveAsPNG(file); // System.Runtime.InteropServices.COMException: '该方法无法用于对那个对象。'
                    }
                }
            }

            document.Close();
        }
    }
}
