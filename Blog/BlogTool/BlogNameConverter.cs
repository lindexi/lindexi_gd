using System;
using System.Text;
using System.Linq;

namespace BlogTool
{
    public class BlogNameConverter
    {
        public static string ConvertTitleToFileName(string title, string replaceText = "-")
        {
            title = MakeValidFileName(title, replaceText);
            var replaceList = new[]
            {
                " ",
                ".",
                "#",
            };

            foreach (var str in replaceList)
            {
                title = title.Replace(str, replaceText);
            }

            return title;
        }

        private static string MakeValidFileName(string text, string replacement = "_")
        {
            StringBuilder str = new StringBuilder();
            var invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in text)
            {
                if (invalidFileNameChars.Contains(c))
                {
                    str.Append(replacement ?? "");
                }
                else
                {
                    str.Append(c);
                }
            }

            return str.ToString();
        }

    }
}
