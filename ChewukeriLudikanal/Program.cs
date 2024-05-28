using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpFont;

namespace ChewukeriLudikanal
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var nugetFolder = Path.Combine(folderPath, @"..\.nuget\packages\sharpfont.dependencies");
            // 如果自己的 nuget 没有设置为其他路径的话
            var sharpFontDependenciesNuGetFolder = Directory.EnumerateDirectories(nugetFolder).First();

            if (Environment.Is64BitProcess)
            {
                var libraryFolder = Path.Combine(sharpFontDependenciesNuGetFolder, @"bin\msvc12\x64\");
                SetDllDirectory(libraryFolder);
            }
            else
            {
                var libraryFolder = Path.Combine(sharpFontDependenciesNuGetFolder, @"bin\msvc12\x86\");
                SetDllDirectory(libraryFolder);
            }

            var library = new Library();
            var face = new Face(library, @"C:\windows\fonts\simfang.ttf");

            uint glyphIndex = face.GetCharIndex('林');

            // 设置字体大小，修复 SharpFont.FreeTypeException:“FreeType error: Invalid size handle.”
            face.SetCharSize(26,0,96,0);

            // 加载 slot 用于后续渲染
            face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);

            // 获取字体信息
            float advanceX = (float) face.Glyph.Advance.X; // same as the advance in metrics
            float bearingX = (float) face.Glyph.Metrics.HorizontalBearingX;
            float width = face.Glyph.Metrics.Width.ToSingle();
            float glyphTop = (float) face.Glyph.Metrics.HorizontalBearingY;
            float glyphBottom = (float) (face.Glyph.Metrics.Height - face.Glyph.Metrics.HorizontalBearingY);

            // 尝试获取字间距
            //kern = (float) face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Default).X;
            face.Glyph.RenderGlyph(RenderMode.Normal);

            face.Glyph.Bitmap.ToGdipBitmap().Save("1.png");
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);
    }
}
