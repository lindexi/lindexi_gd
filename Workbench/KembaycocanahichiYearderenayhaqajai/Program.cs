// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Xml;

var text = "Hello, World!你好世界\u0001abc";

//for (var i = 0; i < text.Length; i++)
//{
//    var c = text[i];
//    if (XmlConvert.IsXmlChar(c))
//    {
//        Console.WriteLine($"'{c}' is a valid XML character.");
//    }
//    else
//    {
//        Console.WriteLine($"'{c}' is not a valid XML character.");
//    }
//}

Console.WriteLine(XmlSafeTextContentHelper.ToSafeXmlText(text));


internal static class XmlSafeTextContentHelper
{
    public static string ToSafeXmlText(string text, char replaceChar = '_')
    {
        // 第一遍是跑一下，因为正常是不会有奇怪的字符的，那就进入快速分支
        // 如果能够找到首个奇怪的字符，则进入慢分支，重新拼装替代字符

        var index = -1;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!XmlConvert.IsXmlChar(c))
            {
                index = i;
                break;
            }
        }

        // 不能使用 EncodeName 去跑
        // 输入： `Hello, World!你好世界\u0001`
        // 输出： Hello_x002C__x0020_World_x0021_你好世界_x0001_
        //XmlConvert.EncodeName()

        var canFindInvalidChar = index >= 0;
        if (canFindInvalidChar)
        {
            // 慢分支，开始重新拼装字符串
            var stringBuilder = new StringBuilder(text.Length);
            stringBuilder.Append(text, 0, index);
            for (var i = index; i < text.Length; i++)
            {
                var c = text[i];
                if (XmlConvert.IsXmlChar(c))
                {
                    stringBuilder.Append(c);
                }
                else
                {
                    stringBuilder.Append(replaceChar);
                }
            }

            return stringBuilder.ToString();
        }
        else
        {
            return text;
        }
    }
}