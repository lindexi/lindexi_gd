using System;
using System.Collections.Generic;
using System.IO;

using ColorCode.Common;

namespace LightTextEditorPlus.Highlighters;

/// <summary>
/// 根据文件信息选择合适的文档高亮定义。
/// </summary>
public static class DocumentHighlighterSelector
{
    private static readonly Dictionary<string, DocumentHighlightDefinition> ExtensionMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        [".md"] = DocumentHighlightDefinition.Markdown,
        [".markdown"] = DocumentHighlightDefinition.Markdown,
        [".mdown"] = DocumentHighlightDefinition.Markdown,
        [".mkd"] = DocumentHighlightDefinition.Markdown,
        [".mkdn"] = DocumentHighlightDefinition.Markdown,
        [".mdtxt"] = DocumentHighlightDefinition.Markdown,

        [".cs"] = DocumentHighlightDefinition.CSharp,
        [".csx"] = DocumentHighlightDefinition.CSharp,

        [".asax"] = DocumentHighlightDefinition.CreateOther(LanguageId.Asax),
        [".ashx"] = DocumentHighlightDefinition.CreateOther(LanguageId.Ashx),
        [".aspx"] = DocumentHighlightDefinition.CreateOther(LanguageId.Aspx),
        [".ascx"] = DocumentHighlightDefinition.CreateOther(LanguageId.Aspx),
        [".master"] = DocumentHighlightDefinition.CreateOther(LanguageId.Aspx),
        [".cpp"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".cc"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".cxx"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".c"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".h"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".hpp"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".hh"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".hxx"] = DocumentHighlightDefinition.CreateOther(LanguageId.Cpp),
        [".css"] = DocumentHighlightDefinition.CreateOther(LanguageId.Css),
        [".fs"] = DocumentHighlightDefinition.CreateOther(LanguageId.FSharp),
        [".fsi"] = DocumentHighlightDefinition.CreateOther(LanguageId.FSharp),
        [".fsx"] = DocumentHighlightDefinition.CreateOther(LanguageId.FSharp),
        [".fsscript"] = DocumentHighlightDefinition.CreateOther(LanguageId.FSharp),
        [".f90"] = DocumentHighlightDefinition.CreateOther(LanguageId.Fortran),
        [".f95"] = DocumentHighlightDefinition.CreateOther(LanguageId.Fortran),
        [".f03"] = DocumentHighlightDefinition.CreateOther(LanguageId.Fortran),
        [".for"] = DocumentHighlightDefinition.CreateOther(LanguageId.Fortran),
        [".f"] = DocumentHighlightDefinition.CreateOther(LanguageId.Fortran),
        [".hs"] = DocumentHighlightDefinition.CreateOther(LanguageId.Haskell),
        [".lhs"] = DocumentHighlightDefinition.CreateOther(LanguageId.Haskell),
        [".html"] = DocumentHighlightDefinition.CreateOther(LanguageId.Html),
        [".htm"] = DocumentHighlightDefinition.CreateOther(LanguageId.Html),
        [".shtml"] = DocumentHighlightDefinition.CreateOther(LanguageId.Html),
        [".xhtml"] = DocumentHighlightDefinition.CreateOther(LanguageId.Html),
        [".java"] = DocumentHighlightDefinition.CreateOther(LanguageId.Java),
        [".js"] = DocumentHighlightDefinition.CreateOther(LanguageId.JavaScript),
        [".mjs"] = DocumentHighlightDefinition.CreateOther(LanguageId.JavaScript),
        [".cjs"] = DocumentHighlightDefinition.CreateOther(LanguageId.JavaScript),
        [".json"] = DocumentHighlightDefinition.CreateOther("json"),
        [".koka"] = DocumentHighlightDefinition.CreateOther(LanguageId.Koka),
        [".matlab"] = DocumentHighlightDefinition.CreateOther(LanguageId.MatLab),
        [".m"] = DocumentHighlightDefinition.CreateOther(LanguageId.MatLab),
        [".php"] = DocumentHighlightDefinition.CreateOther(LanguageId.Php),
        [".php3"] = DocumentHighlightDefinition.CreateOther(LanguageId.Php),
        [".php4"] = DocumentHighlightDefinition.CreateOther(LanguageId.Php),
        [".php5"] = DocumentHighlightDefinition.CreateOther(LanguageId.Php),
        [".phtml"] = DocumentHighlightDefinition.CreateOther(LanguageId.Php),
        [".ps1"] = DocumentHighlightDefinition.CreateOther(LanguageId.PowerShell),
        [".psm1"] = DocumentHighlightDefinition.CreateOther(LanguageId.PowerShell),
        [".psd1"] = DocumentHighlightDefinition.CreateOther(LanguageId.PowerShell),
        [".py"] = DocumentHighlightDefinition.CreateOther(LanguageId.Python),
        [".pyw"] = DocumentHighlightDefinition.CreateOther(LanguageId.Python),
        [".sql"] = DocumentHighlightDefinition.CreateOther(LanguageId.Sql),
        [".ts"] = DocumentHighlightDefinition.CreateOther(LanguageId.TypeScript),
        [".tsx"] = DocumentHighlightDefinition.CreateOther(LanguageId.TypeScript),
        [".vb"] = DocumentHighlightDefinition.CreateOther(LanguageId.VbDotNet),
        [".vbhtml"] = DocumentHighlightDefinition.CreateOther(LanguageId.AspxVb),
        [".xml"] = DocumentHighlightDefinition.CreateOther(LanguageId.Xml),
        [".xaml"] = DocumentHighlightDefinition.CreateOther(LanguageId.Xml),
        [".axaml"] = DocumentHighlightDefinition.CreateOther(LanguageId.Xml),
        [".svg"] = DocumentHighlightDefinition.CreateOther(LanguageId.Xml),
        [".cshtml"] = DocumentHighlightDefinition.CreateOther(LanguageId.AspxCs),
    };

    /// <summary>
    /// 根据文件信息获取文档高亮定义。
    /// </summary>
    /// <param name="fileInfo">文件信息。</param>
    /// <returns>匹配的高亮定义。</returns>
    public static DocumentHighlightDefinition GetDocumentHighlightDefinition(FileInfo? fileInfo)
    {
        if (fileInfo is null)
        {
            return DocumentHighlightDefinition.Markdown;
        }

        return GetDocumentHighlightDefinition(fileInfo.Extension);
    }

    /// <summary>
    /// 根据扩展名或文件路径获取文档高亮定义。
    /// </summary>
    /// <param name="extension">扩展名或文件路径。</param>
    /// <returns>匹配的高亮定义。</returns>
    public static DocumentHighlightDefinition GetDocumentHighlightDefinition(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return DocumentHighlightDefinition.Markdown;
        }

        if (!extension.StartsWith(".", StringComparison.Ordinal))
        {
            extension = Path.GetExtension(extension);
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            return DocumentHighlightDefinition.Markdown;
        }

        return ExtensionMapping.GetValueOrDefault(extension, DocumentHighlightDefinition.Markdown);
    }
}
