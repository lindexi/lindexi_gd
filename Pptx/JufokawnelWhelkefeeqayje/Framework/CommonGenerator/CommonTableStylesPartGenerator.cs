using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonTableStylesPartGenerator
    {
        public static TableStylesPart GenerateTableStylesPart(this PresentationPart presentationPart)
        {
            var (tableStylesPart1, _) = presentationPart.AddNewPartWithGenerateId<TableStylesPart>();
            GenerateTableStylesPartContent(tableStylesPart1);
            return tableStylesPart1;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="tableStylesPart1"></param>
        // Generates content of tableStylesPart1.
        private static void GenerateTableStylesPartContent(TableStylesPart tableStylesPart1)
        {
            A.TableStyleList tableStyleList1 =
                new A.TableStyleList() { Default = "{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}" };
            tableStyleList1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

            tableStylesPart1.TableStyleList = tableStyleList1;
        }
    }
}
