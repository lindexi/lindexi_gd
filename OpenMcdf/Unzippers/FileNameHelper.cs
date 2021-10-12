using System.IO;
using System.Text;

namespace OpenMcdf
{
    static class FileNameHelper
    {
        public static string FixFileName(string name)
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in name)
            {
                if (c < byte.MaxValue)
                {
                    if ((c >= 'A' && c <= 'Z')
                        || (c >= 'a' && c <= 'z')
                        || (c >= '0' && c <= '9'))
                    {
                        stringBuilder.Append(c);
                    }
                    else
                    {
                        stringBuilder.Append(((int) c).ToString());
                    }
                }
            }

            foreach (var invalidPathChar in Path.GetInvalidPathChars())
            {
                stringBuilder.Replace(invalidPathChar, '_');
            }

            return stringBuilder.ToString();
        }
    }
}