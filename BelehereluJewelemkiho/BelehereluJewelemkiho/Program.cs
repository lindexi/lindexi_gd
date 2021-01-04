using System;
using System.IO;
using System.IO.Packaging;
using DocumentFormat.OpenXml.Packaging;

namespace BelehereluJewelemkiho
{
    class Program
    {
        static void Main(string[] args)
        {
            const string fileName = "Excel.xlsx";

            var openSettings = new OpenSettings()
            {
                RelationshipErrorHandlerFactory = RelationshipErrorHandler.CreateRewriterFactory(Rewriter)
            };

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(fs, isEditable: true, openSettings))
                {

                }
            }
        }

        /// <summary>
        /// 表示如何重写修复超链接格式
        /// </summary>
        /// <param name="partUri">这个 <paramref name="uri"/> 属于哪个文档 Part 内容，值如 /xl/worksheets/_rels/sheet1.xml.rels 等</param>
        /// <param name="id">这个资源的值</param>
        /// <param name="uri">格式不对的 Uri 内容</param>
        /// <returns></returns>
        static string Rewriter(Uri partUri, string id, string uri)
            => $"http://unknown?id={id}";
    }
}
