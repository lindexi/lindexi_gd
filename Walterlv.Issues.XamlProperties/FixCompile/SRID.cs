using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xaml.Resources;
using Microsoft.Build.Framework;

namespace System.Xaml.Resources
{
    public class Strings
    {

    }
}

class FakeResourceManager : global::System.Resources.ResourceManager
{
    public override string GetString(string name)
    {
        return name;
    }

    public override string GetString(string name, CultureInfo culture)
    {
        return name;
    }

    public override object GetObject(string name)
    {
        return name;
    }

    public override object GetObject(string name, CultureInfo culture)
    {
        return name;
    }
}

   public static class SRID
    {
            private static global::System.Resources.ResourceManager resourceMan;

            private static global::System.Globalization.CultureInfo resourceCulture;

            /// <summary>
            ///   返回此类使用的缓存的 ResourceManager 实例。
            /// </summary>
            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
            internal static global::System.Resources.ResourceManager ResourceManager
            {
                get
                {
                    if (object.ReferenceEquals(resourceMan, null))
                    {
                        resourceMan = new FakeResourceManager();
                    }
                    return resourceMan;
                }
            }

            /// <summary>
            ///   重写当前线程的 CurrentUICulture 属性
            ///   重写当前线程的 CurrentUICulture 属性。
            /// </summary>
            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
            internal static global::System.Globalization.CultureInfo Culture
            {
                get
                {
                    return resourceCulture;
                }
                set
                {
                    resourceCulture = value;
                }
            }

    /// <summary>
    ///   查找类似 Analysis Result : &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string AnalysisResult
    {
        get
        {
            return ResourceManager.GetString("AnalysisResult", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC1002: Library project file cannot specify ApplicationDefinition element. 的本地化字符串。
    /// </summary>
    internal static string AppDefIsNotRequired
    {
        get
        {
            return ResourceManager.GetString("AppDefIsNotRequired", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Input: Markup ApplicationDefinition file: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ApplicationDefinitionFile
    {
        get
        {
            return ResourceManager.GetString("ApplicationDefinitionFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6015: &apos;{0}&apos; cannot be set on the &apos;{1}:{2}&apos; tag. 的本地化字符串。
    /// </summary>
    internal static string AttributeNotAllowedOnCodeTag
    {
        get
        {
            return ResourceManager.GetString("AttributeNotAllowedOnCodeTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 UM1001: Unrecognized UidManager task name &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string BadUidTask
    {
        get
        {
            return ResourceManager.GetString("BadUidTask", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Checking Uids in file &apos;{0}&apos; ... 的本地化字符串。
    /// </summary>
    internal static string CheckingUids
    {
        get
        {
            return ResourceManager.GetString("CheckingUids", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Generated localization directives file: &apos;{0}&apos; . 的本地化字符串。
    /// </summary>
    internal static string CommentFileGenerated
    {
        get
        {
            return ResourceManager.GetString("CommentFileGenerated", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Generating localization directives file: &apos;{0}&apos; ... 的本地化字符串。
    /// </summary>
    internal static string CommentFileGenerating
    {
        get
        {
            return ResourceManager.GetString("CommentFileGenerating", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Markup compilation is done. 的本地化字符串。
    /// </summary>
    internal static string CompilationDone
    {
        get
        {
            return ResourceManager.GetString("CompilationDone", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MarkupCompilePass1 successfully generated BAML or source code files. 的本地化字符串。
    /// </summary>
    internal static string CompileSucceed_Pass1
    {
        get
        {
            return ResourceManager.GetString("CompileSucceed_Pass1", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MarkupCompilePass2 successfully generated BAML or source code files. 的本地化字符串。
    /// </summary>
    internal static string CompileSucceed_Pass2
    {
        get
        {
            return ResourceManager.GetString("CompileSucceed_Pass2", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6011: &apos;{0}&apos; event has a generic event handler delegate type &apos;{1}&apos;. The type parameters of &apos;{1}&apos; cannot be bound with an appropriate type argument because the containing tag &apos;{2}&apos; is not a generic type. 的本地化字符串。
    /// </summary>
    internal static string ContainingTagNotGeneric
    {
        get
        {
            return ResourceManager.GetString("ContainingTagNotGeneric", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Current project directory is &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string CurrentDirectory
    {
        get
        {
            return ResourceManager.GetString("CurrentDirectory", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6022: Only a root tag can specify attribute &apos;{0}:{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string DefinitionAttributeNotAllowed
    {
        get
        {
            return ResourceManager.GetString("DefinitionAttributeNotAllowed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6010: &apos;{0}:{1}&apos; cannot be specified as the root element. 的本地化字符串。
    /// </summary>
    internal static string DefinitionTagNotAllowedAtRoot
    {
        get
        {
            return ResourceManager.GetString("DefinitionTagNotAllowedAtRoot", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6004: &apos;{0}:{1}&apos; contains &apos;{2}&apos;. &apos;{0}:{1}&apos; can contain only a CDATA or text section. 的本地化字符串。
    /// </summary>
    internal static string DefnTagsCannotBeNested
    {
        get
        {
            return ResourceManager.GetString("DefnTagsCannotBeNested", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Started the markup compilation. 的本地化字符串。
    /// </summary>
    internal static string DoCompilation
    {
        get
        {
            return ResourceManager.GetString("DoCompilation", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6030: {0}=&quot;{1}&quot; is not valid. &apos;{0}&apos; event attribute value cannot be a string that is empty or has only white space. 的本地化字符串。
    /// </summary>
    internal static string EmptyEventStringNotAllowed
    {
        get
        {
            return ResourceManager.GetString("EmptyEventStringNotAllowed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6021: {0}:FieldModifier cannot be specified on this tag because it has either no {0}:Name or Name attribute set, or the tag is locally defined and has a Name attribute set, which is not allowed. 的本地化字符串。
    /// </summary>
    internal static string FieldModifierNotAllowed
    {
        get
        {
            return ResourceManager.GetString("FieldModifierNotAllowed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 BG1002: File &apos;{0}&apos; cannot be found. 的本地化字符串。
    /// </summary>
    internal static string FileNotFound
    {
        get
        {
            return ResourceManager.GetString("FileNotFound", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Input file &apos;{0}&apos; is resolved to new relative path &apos;{1}&apos; at directory &apos;{2}&apos;. 的本地化字符串。
    /// </summary>
    internal static string FileResolved
    {
        get
        {
            return ResourceManager.GetString("FileResolved", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 UM1004: {0} files failed Uid check. 的本地化字符串。
    /// </summary>
    internal static string FilesFailedUidCheck
    {
        get
        {
            return ResourceManager.GetString("FilesFailedUidCheck", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Uids valid in {0} files. 的本地化字符串。
    /// </summary>
    internal static string FilesPassedUidCheck
    {
        get
        {
            return ResourceManager.GetString("FilesPassedUidCheck", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Uids removed from {0} files. 的本地化字符串。
    /// </summary>
    internal static string FilesRemovedUid
    {
        get
        {
            return ResourceManager.GetString("FilesRemovedUid", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Uids updated in {0} files. 的本地化字符串。
    /// </summary>
    internal static string FilesUpdatedUid
    {
        get
        {
            return ResourceManager.GetString("FilesUpdatedUid", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Generated BAML file: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string GeneratedBamlFile
    {
        get
        {
            return ResourceManager.GetString("GeneratedBamlFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Generated code file: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string GeneratedCodeFile
    {
        get
        {
            return ResourceManager.GetString("GeneratedCodeFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6003: &apos;{0}:{1}&apos; contains &apos;{2}&apos;. &apos;{0}:{1}&apos; cannot contain nested content. 的本地化字符串。
    /// </summary>
    internal static string IllegalCDataTextScoping
    {
        get
        {
            return ResourceManager.GetString("IllegalCDataTextScoping", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 UM1006: &apos;{0}&apos; directory does not exist and cannot be created. 的本地化字符串。
    /// </summary>
    internal static string IntermediateDirectoryError
    {
        get
        {
            return ResourceManager.GetString("IntermediateDirectoryError", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 InternalTypeHelper class is not required for this project, make file &apos;{0}&apos; empty. 的本地化字符串。
    /// </summary>
    internal static string InternalTypeHelperNotRequired
    {
        get
        {
            return ResourceManager.GetString("InternalTypeHelperNotRequired", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6018: &apos;{0}&apos; class name is not valid for the locally defined XAML root element. 的本地化字符串。
    /// </summary>
    internal static string InvalidBaseClassName
    {
        get
        {
            return ResourceManager.GetString("InvalidBaseClassName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6019: &apos;{0}&apos; namespace is not valid for the locally defined XAML root element &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string InvalidBaseClassNamespace
    {
        get
        {
            return ResourceManager.GetString("InvalidBaseClassNamespace", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6027: {0}:{1}=&quot;{2}&quot; is not valid. &apos;{2}&apos; is not a valid {3}class name. 的本地化字符串。
    /// </summary>
    internal static string InvalidClassName
    {
        get
        {
            return ResourceManager.GetString("InvalidClassName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 FC1001: The UICulture value &apos;{0}&apos; set in the project file is not valid. 的本地化字符串。
    /// </summary>
    internal static string InvalidCulture
    {
        get
        {
            return ResourceManager.GetString("InvalidCulture", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4402: Serializer does not support custom BAML serialization operations. 的本地化字符串。
    /// </summary>
    internal static string InvalidCustomSerialize
    {
        get
        {
            return ResourceManager.GetString("InvalidCustomSerialize", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6029: &apos;{0}&apos; name is not valid in the default namespace &apos;{1}&apos;. Correct the RootNamespace tag value in the project file. 的本地化字符串。
    /// </summary>
    internal static string InvalidDefaultCLRNamespace
    {
        get
        {
            return ResourceManager.GetString("InvalidDefaultCLRNamespace", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4401: Serializer does not support Convert operations. 的本地化字符串。
    /// </summary>
    internal static string InvalidDeSerialize
    {
        get
        {
            return ResourceManager.GetString("InvalidDeSerialize", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6005: {0}=&quot;{1}&quot; is not valid. &apos;{1}&apos; is not a valid event handler method name. Only instance methods on the generated or code-behind class are valid. 的本地化字符串。
    /// </summary>
    internal static string InvalidEventHandlerName
    {
        get
        {
            return ResourceManager.GetString("InvalidEventHandlerName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 LC1004: Localizability attribute setting &apos;{0}&apos; is not valid. 的本地化字符串。
    /// </summary>
    internal static string InvalidLocalizabilityValue
    {
        get
        {
            return ResourceManager.GetString("InvalidLocalizabilityValue", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 LC1001: Localization comment target property is not valid in string &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string InvalidLocCommentTarget
    {
        get
        {
            return ResourceManager.GetString("InvalidLocCommentTarget", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 LC1002: Localization comment value is not valid for target property &apos;{0}&apos; in string &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string InvalidLocCommentValue
    {
        get
        {
            return ResourceManager.GetString("InvalidLocCommentValue", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6001: Markup file is not valid. Specify a source markup file with an .xaml extension. 的本地化字符串。
    /// </summary>
    internal static string InvalidMarkupFile
    {
        get
        {
            return ResourceManager.GetString("InvalidMarkupFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6028: {0}:TypeArguments=&quot;{1}&quot; is not valid. &apos;{2}&apos; is not a valid type name reference for the generic argument at position &apos;{3}&apos;. 的本地化字符串。
    /// </summary>
    internal static string InvalidTypeName
    {
        get
        {
            return ResourceManager.GetString("InvalidTypeName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3000: &apos;{0}&apos; XML is not valid. 的本地化字符串。
    /// </summary>
    internal static string InvalidXml
    {
        get
        {
            return ResourceManager.GetString("InvalidXml", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6023: Because &apos;{0}&apos; is implemented in the same assembly, you must set the {1}:Name attribute rather than the Name attribute. 的本地化字符串。
    /// </summary>
    internal static string LocalNamePropertyNotAllowed
    {
        get
        {
            return ResourceManager.GetString("LocalNamePropertyNotAllowed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Input: Local reference markup ApplicationDefinition file is &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string LocalRefAppDefFile
    {
        get
        {
            return ResourceManager.GetString("LocalRefAppDefFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Generated BAML file: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string LocalRefGeneratedBamlFile
    {
        get
        {
            return ResourceManager.GetString("LocalRefGeneratedBamlFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Input: Local reference markup Page file: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string LocalRefMarkupPage
    {
        get
        {
            return ResourceManager.GetString("LocalRefMarkupPage", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6012: &apos;{0}&apos; event has a generic event handler delegate type &apos;{1}&apos;. The type parameter &apos;{2}&apos; on &apos;{1}&apos; does not match any type parameters on the containing generic tag &apos;{3}&apos;. 的本地化字符串。
    /// </summary>
    internal static string MatchingTypeArgsNotFoundInRefType
    {
        get
        {
            return ResourceManager.GetString("MatchingTypeArgsNotFoundInRefType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6026: &apos;{0}&apos; root element requires a {1}:Class attribute because &apos;{2}&apos; contains a {1}:Code tag. Either remove {1}:Code and its contents, or add a {1}:Class attribute to the root element. 的本地化字符串。
    /// </summary>
    internal static string MissingClassDefinitionForCodeTag
    {
        get
        {
            return ResourceManager.GetString("MissingClassDefinitionForCodeTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6024: &apos;{0}&apos; root element requires a {1}:Class attribute to support event handlers in the XAML file. Either remove the event handler for the {2} event, or add a {1}:Class attribute to the root element. 的本地化字符串。
    /// </summary>
    internal static string MissingClassDefinitionForEvent
    {
        get
        {
            return ResourceManager.GetString("MissingClassDefinitionForEvent", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6025: &apos;{0}&apos; root element is a generic type and requires a {1}:Class attribute to support the {1}:TypeArguments attribute specified on the root element tag. 的本地化字符串。
    /// </summary>
    internal static string MissingClassDefinitionForTypeArgs
    {
        get
        {
            return ResourceManager.GetString("MissingClassDefinitionForTypeArgs", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6031: {0}:FieldModifier attribute cannot be specified because a {0}:Class attribute is also required to generate a Name field with the specified access modifier. Either add a {0}:Class attribute on the root tag or remove the {0}:FieldModifier attribute. 的本地化字符串。
    /// </summary>
    internal static string MissingClassWithFieldModifier
    {
        get
        {
            return ResourceManager.GetString("MissingClassWithFieldModifier", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6009: {0}:ClassModifier attribute cannot be specified on the root tag because a {0}:Class attribute is also required. Either add a {0}:Class attribute or remove the {0}:ClassModifier attribute. 的本地化字符串。
    /// </summary>
    internal static string MissingClassWithModifier
    {
        get
        {
            return ResourceManager.GetString("MissingClassWithModifier", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6016: {0}:Class attribute is missing. It is required when a {0}:Subclass attribute is specified. 的本地化字符串。
    /// </summary>
    internal static string MissingClassWithSubClass
    {
        get
        {
            return ResourceManager.GetString("MissingClassWithSubClass", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 RG1002: ResourcesGenerator can generate only one .resources file at a time. The OutputResourcesFile property in the project file must be set to one file. 的本地化字符串。
    /// </summary>
    internal static string MoreResourcesFiles
    {
        get
        {
            return ResourceManager.GetString("MoreResourcesFiles", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC1004: Project file cannot specify more than one SplashScreen element. 的本地化字符串。
    /// </summary>
    internal static string MultipleSplashScreenImages
    {
        get
        {
            return ResourceManager.GetString("MultipleSplashScreenImages", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 UM1002: Uid &quot;{0}&quot; for element &apos;{1}&apos; is not unique. 的本地化字符串。
    /// </summary>
    internal static string MultipleUidUse
    {
        get
        {
            return ResourceManager.GetString("MultipleUidUse", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC1003: Project file cannot specify more than one ApplicationDefinition element. 的本地化字符串。
    /// </summary>
    internal static string MutlipleApplicationFiles
    {
        get
        {
            return ResourceManager.GetString("MutlipleApplicationFiles", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 A &apos;{0}&apos; is named &apos;{1}&apos;. Do not name ResourceDictionary contents because their instantiation is deferred. 的本地化字符串。
    /// </summary>
    internal static string NamedResDictItemWarning
    {
        get
        {
            return ResourceManager.GetString("NamedResDictItemWarning", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 BG1001: Unknown CLS exception. 的本地化字符串。
    /// </summary>
    internal static string NonClsError
    {
        get
        {
            return ResourceManager.GetString("NonClsError", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 OutputType is &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string OutputType
    {
        get
        {
            return ResourceManager.GetString("OutputType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3098: Unexpected token &apos;{0}&apos; at position &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string Parser_UnexpectedToken
    {
        get
        {
            return ResourceManager.GetString("Parser_UnexpectedToken", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3001: TypeConverter syntax error encountered while processing initialization string &apos;{0}&apos;. Property elements are not allowed on objects created via TypeConverter. 的本地化字符串。
    /// </summary>
    internal static string ParserAbandonedTypeConverterText
    {
        get
        {
            return ResourceManager.GetString("ParserAbandonedTypeConverterText", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3099: Cannot load assembly &apos;{0}&apos; because a different version of that same assembly is loaded &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserAssemblyLoadVersionMismatch
    {
        get
        {
            return ResourceManager.GetString("ParserAssemblyLoadVersionMismatch", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3002: The AsyncRecords attribute must be set on the root tag. 的本地化字符串。
    /// </summary>
    internal static string ParserAsyncOnRoot
    {
        get
        {
            return ResourceManager.GetString("ParserAsyncOnRoot", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3015: The attached property &apos;{0}&apos; is not defined on &apos;{1}&apos; or one of its base classes. 的本地化字符串。
    /// </summary>
    internal static string ParserAttachedPropInheritError
    {
        get
        {
            return ResourceManager.GetString("ParserAttachedPropInheritError", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3003: There are too many attributes specified for &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserAttributeArgsHigh
    {
        get
        {
            return ResourceManager.GetString("ParserAttributeArgsHigh", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3004: There are not enough attributes specified for &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserAttributeArgsLow
    {
        get
        {
            return ResourceManager.GetString("ParserAttributeArgsLow", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3005: The property &apos;{0}&apos; must be in the default namespace or in the element namespace &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserAttributeNamespaceMisMatch
    {
        get
        {
            return ResourceManager.GetString("ParserAttributeNamespaceMisMatch", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3006: Mapper.SetAssemblyPath cannot accept an empty assemblyName. 的本地化字符串。
    /// </summary>
    internal static string ParserBadAssemblyName
    {
        get
        {
            return ResourceManager.GetString("ParserBadAssemblyName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3007: Mapper.SetAssemblyPath cannot accept an empty assemblyPath. 的本地化字符串。
    /// </summary>
    internal static string ParserBadAssemblyPath
    {
        get
        {
            return ResourceManager.GetString("ParserBadAssemblyPath", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3008: An element of type &apos;{0}&apos; cannot be set on the complex property &apos;{1}&apos;. They are not compatible types. 的本地化字符串。
    /// </summary>
    internal static string ParserBadChild
    {
        get
        {
            return ResourceManager.GetString("ParserBadChild", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3009: Cannot find a public constructor for &apos;{0}&apos; that takes {1} arguments. 的本地化字符串。
    /// </summary>
    internal static string ParserBadConstructorParams
    {
        get
        {
            return ResourceManager.GetString("ParserBadConstructorParams", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3012: A key for a dictionary cannot be of type &apos;{0}&apos;. Only String, TypeExtension, and StaticExtension are supported. 的本地化字符串。
    /// </summary>
    internal static string ParserBadKey
    {
        get
        {
            return ResourceManager.GetString("ParserBadKey", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3029: &apos;{0}&apos; member is not valid because it does not have a qualifying type name. 的本地化字符串。
    /// </summary>
    internal static string ParserBadMemberReference
    {
        get
        {
            return ResourceManager.GetString("ParserBadMemberReference", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3010: &apos;{0}&apos; Name property value is not valid. Name must start with a letter or an underscore and can contain only letters, digits, and underscores. 的本地化字符串。
    /// </summary>
    internal static string ParserBadName
    {
        get
        {
            return ResourceManager.GetString("ParserBadName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3094: Cannot convert string value &apos;{0}&apos; to type &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserBadString
    {
        get
        {
            return ResourceManager.GetString("ParserBadString", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3013: SynchronousMode property value is not valid. Valid values are Async and Sync. 的本地化字符串。
    /// </summary>
    internal static string ParserBadSyncMode
    {
        get
        {
            return ResourceManager.GetString("ParserBadSyncMode", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3014: The object being added to an array property is not a valid type. The array is of type &apos;{0}&apos; but the object being added is of type &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserBadTypeInArrayProperty
    {
        get
        {
            return ResourceManager.GetString("ParserBadTypeInArrayProperty", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3079: MarkupExtensions are not allowed for Uid or Name property values, so &apos;{0}&apos; is not valid. 的本地化字符串。
    /// </summary>
    internal static string ParserBadUidOrNameME
    {
        get
        {
            return ResourceManager.GetString("ParserBadUidOrNameME", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3028: Cannot add content to object of type &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserCannotAddAnyChildren
    {
        get
        {
            return ResourceManager.GetString("ParserCannotAddAnyChildren", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3089: The object &apos;{0}&apos; already has a child and cannot add &apos;{1}&apos;. &apos;{0}&apos; can accept only one child. 的本地化字符串。
    /// </summary>
    internal static string ParserCanOnlyHaveOneChild
    {
        get
        {
            return ResourceManager.GetString("ParserCanOnlyHaveOneChild", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3081: &apos;{0}&apos; is a read-only property of type IList or IDictionary and cannot be set because it does not have a public or internal get accessor. 的本地化字符串。
    /// </summary>
    internal static string ParserCantGetProperty
    {
        get
        {
            return ResourceManager.GetString("ParserCantGetProperty", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3080: The {0} &apos;{1}&apos; cannot be set because it does not have an accessible {2} accessor. 的本地化字符串。
    /// </summary>
    internal static string ParserCantSetAttribute
    {
        get
        {
            return ResourceManager.GetString("ParserCantSetAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3087: Cannot set content property &apos;{0}&apos; on element &apos;{1}&apos;. &apos;{0}&apos; has incorrect access level or its assembly does not allow access. 的本地化字符串。
    /// </summary>
    internal static string ParserCantSetContentProperty
    {
        get
        {
            return ResourceManager.GetString("ParserCantSetContentProperty", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3082: &apos;{0}&apos; cannot be set as the value of a Trigger&apos;s Property attribute because it does not have a public or internal get accessor. 的本地化字符串。
    /// </summary>
    internal static string ParserCantSetTriggerCondition
    {
        get
        {
            return ResourceManager.GetString("ParserCantSetTriggerCondition", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3016: Two new namespaces cannot be compatible with the same old namespace using an XmlnsCompatibility attribute.�&apos;{0}&apos; namespace is already marked compatible with &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserCompatDuplicate
    {
        get
        {
            return ResourceManager.GetString("ParserCompatDuplicate", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3088: Property elements cannot be in the middle of an element&apos;s content.  They must be before or after the content. 的本地化字符串。
    /// </summary>
    internal static string ParserContentMustBeContiguous
    {
        get
        {
            return ResourceManager.GetString("ParserContentMustBeContiguous", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3061: The Element type &apos;{0}&apos; does not have an associated TypeConverter to parse the string &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserDefaultConverterElement
    {
        get
        {
            return ResourceManager.GetString("ParserDefaultConverterElement", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3017: &apos;Code&apos; tag from xaml namespace found in XAML file. To load this file, you must compile it. 的本地化字符串。
    /// </summary>
    internal static string ParserDefTag
    {
        get
        {
            return ResourceManager.GetString("ParserDefTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3018: Cannot modify data in a sealed XmlnsDictionary. 的本地化字符串。
    /// </summary>
    internal static string ParserDictionarySealed
    {
        get
        {
            return ResourceManager.GetString("ParserDictionarySealed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3020: The dictionary key &apos;{0}&apos; is already used. Key attributes are used as keys when inserting objects into a dictionary and must be unique. 的本地化字符串。
    /// </summary>
    internal static string ParserDupDictionaryKey
    {
        get
        {
            return ResourceManager.GetString("ParserDupDictionaryKey", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3033: The property &apos;{0}&apos; has already been set on this markup extension and can only be set once. 的本地化字符串。
    /// </summary>
    internal static string ParserDuplicateMarkupExtensionProperty
    {
        get
        {
            return ResourceManager.GetString("ParserDuplicateMarkupExtensionProperty", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3024: &apos;{0}&apos; property has already been set and can be set only once. 的本地化字符串。
    /// </summary>
    internal static string ParserDuplicateProperty1
    {
        get
        {
            return ResourceManager.GetString("ParserDuplicateProperty1", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3025: Property &apos;{0}&apos; and &apos;{1}&apos; refer to the same property. Duplicate property settings are not allowed. 的本地化字符串。
    /// </summary>
    internal static string ParserDuplicateProperty2
    {
        get
        {
            return ResourceManager.GetString("ParserDuplicateProperty2", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3026: &apos;{0}&apos; property element cannot be empty. It must contain child elements or text. 的本地化字符串。
    /// </summary>
    internal static string ParserEmptyComplexProp
    {
        get
        {
            return ResourceManager.GetString("ParserEmptyComplexProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3027: The EntityReference &amp;{0}; is not recognized. 的本地化字符串。
    /// </summary>
    internal static string ParserEntityReference
    {
        get
        {
            return ResourceManager.GetString("ParserEntityReference", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3083: Cannot access the delegate type &apos;{0}&apos; for the &apos;{1}&apos; event. &apos;{0}&apos; has incorrect access level or its assembly does not allow access. 的本地化字符串。
    /// </summary>
    internal static string ParserEventDelegateTypeNotAccessible
    {
        get
        {
            return ResourceManager.GetString("ParserEventDelegateTypeNotAccessible", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3030: &apos;{0}&apos; property is a read-only IEnumerable property, which means that &apos;{1}&apos; must implement IAddChild. 的本地化字符串。
    /// </summary>
    internal static string ParserIEnumerableIAddChild
    {
        get
        {
            return ResourceManager.GetString("ParserIEnumerableIAddChild", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3078: Invalid ContentPropertyAttribute on type &apos;{0}&apos;, property &apos;{1}&apos; not found. 的本地化字符串。
    /// </summary>
    internal static string ParserInvalidContentPropertyAttribute
    {
        get
        {
            return ResourceManager.GetString("ParserInvalidContentPropertyAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Known type value {0}=&apos;{1}&apos; is not a valid known type. 的本地化字符串。
    /// </summary>
    internal static string ParserInvalidKnownType
    {
        get
        {
            return ResourceManager.GetString("ParserInvalidKnownType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3011: Cannot find the static member &apos;{0}&apos; on the type &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserInvalidStaticMember
    {
        get
        {
            return ResourceManager.GetString("ParserInvalidStaticMember", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3031: Keys and values in XmlnsDictionary must be strings. 的本地化字符串。
    /// </summary>
    internal static string ParserKeysAreStrings
    {
        get
        {
            return ResourceManager.GetString("ParserKeysAreStrings", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Line {0} Position {1} 的本地化字符串。
    /// </summary>
    internal static string ParserLineAndOffset
    {
        get
        {
            return ResourceManager.GetString("ParserLineAndOffset", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3037: Missing XmlNamespace, Assembly or ClrNamespace in Mapping instruction. 的本地化字符串。
    /// </summary>
    internal static string ParserMapPIMissingKey
    {
        get
        {
            return ResourceManager.GetString("ParserMapPIMissingKey", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 &apos;{0}&apos; mapping URI is not valid. 的本地化字符串。
    /// </summary>
    internal static string ParserMappingUriInvalid
    {
        get
        {
            return ResourceManager.GetString("ParserMappingUriInvalid", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3040: Format is not valid for MarkupExtension that specifies constructor arguments in &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionBadConstructorParam
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionBadConstructorParam", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3041: Markup extensions require a single &apos;=&apos; between name and value, and a single &apos;,&apos; between constructor parameters and name/value pairs. The arguments &apos;{0}&apos; are not valid. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionBadDelimiter
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionBadDelimiter", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3091: &apos;{0}&apos; is not valid. Markup extensions require only spaces between the markup extension name and the first parameter. Cannot have comma or equals sign before the first parameter. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionDelimiterBeforeFirstAttribute
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionDelimiterBeforeFirstAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC8001: Encountered a closing BracketCharacter &apos;{0}&apos; at Line Number &apos;{1}&apos; and Line Position &apos;{2}&apos; without a corresponding opening BracketCharacter. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionInvalidClosingBracketCharacers
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionInvalidClosingBracketCharacers", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC8002: BracketCharacter &apos;{0}&apos; at Line Number &apos;{1}&apos; and Line Position &apos;{2}&apos; does not have a corresponding opening/closing BracketCharacter. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionMalformedBracketCharacers
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionMalformedBracketCharacers", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3038: MarkupExtension expressions must end with a &apos;}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionNoEndCurlie
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionNoEndCurlie", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3042: Name/value pairs in MarkupExtensions must have the format &apos;Name = Value&apos; and each pair is separated by a comma. &apos;{0}&apos; does not follow this format. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionNoNameValue
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionNoNameValue", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3043: Names and Values in a MarkupExtension cannot contain quotes. The MarkupExtension arguments &apos;{0}&apos; are not valid. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionNoQuotesInName
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionNoQuotesInName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3044: The text &apos;{1}&apos; is not allowed after the closing &apos;{0}&apos; of a MarkupExtension expression. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionTrailingGarbage
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionTrailingGarbage", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3045: Unknown property &apos;{0}&apos; for type &apos;{1}&apos; encountered while parsing a Markup Extension. 的本地化字符串。
    /// </summary>
    internal static string ParserMarkupExtensionUnknownAttr
    {
        get
        {
            return ResourceManager.GetString("ParserMarkupExtensionUnknownAttr", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3046: Unknown attribute &apos;{0}&apos; in the &apos;{1}&apos; namespace. Note that only the Key attribute is currently supported in this namespace. 的本地化字符串。
    /// </summary>
    internal static string ParserMetroUnknownAttribute
    {
        get
        {
            return ResourceManager.GetString("ParserMetroUnknownAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3047: Internal parser error - Cannot use multiple writable BAML records at the same time. 的本地化字符串。
    /// </summary>
    internal static string ParserMultiBamls
    {
        get
        {
            return ResourceManager.GetString("ParserMultiBamls", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 &apos;{0}&apos; property element cannot be nested directly inside another property element. 的本地化字符串。
    /// </summary>
    internal static string ParserNestedComplexProp
    {
        get
        {
            return ResourceManager.GetString("ParserNestedComplexProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3049: Cannot place attributes on Array tags. 的本地化字符串。
    /// </summary>
    internal static string ParserNoAttrArray
    {
        get
        {
            return ResourceManager.GetString("ParserNoAttrArray", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3021: Asynchronous loading is not supported when compiling a XAML file, so a SynchronousMode of &apos;{0}&apos; is not allowed. 的本地化字符串。
    /// </summary>
    internal static string ParserNoBamlAsync
    {
        get
        {
            return ResourceManager.GetString("ParserNoBamlAsync", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3051: The type &apos;{0}&apos; does not support element content. 的本地化字符串。
    /// </summary>
    internal static string ParserNoChildrenTag
    {
        get
        {
            return ResourceManager.GetString("ParserNoChildrenTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3022: All objects added to an IDictionary must have a Key attribute or some other type of key associated with them. 的本地化字符串。
    /// </summary>
    internal static string ParserNoDictionaryKey
    {
        get
        {
            return ResourceManager.GetString("ParserNoDictionaryKey", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3023: The Key attribute can only be used on a tag contained in a Dictionary (such as a ResourceDictionary). 的本地化字符串。
    /// </summary>
    internal static string ParserNoDictionaryName
    {
        get
        {
            return ResourceManager.GetString("ParserNoDictionaryName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3032: &apos;{1}&apos; cannot be used as a value for &apos;{0}&apos;. Numbers are not valid enumeration values. 的本地化字符串。
    /// </summary>
    internal static string ParserNoDigitEnums
    {
        get
        {
            return ResourceManager.GetString("ParserNoDigitEnums", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3052: XAML file that contains events must be compiled. 的本地化字符串。
    /// </summary>
    internal static string ParserNoEvents
    {
        get
        {
            return ResourceManager.GetString("ParserNoEvents", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3053: Cannot use property element syntax to specify event handler &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserNoEventTag
    {
        get
        {
            return ResourceManager.GetString("ParserNoEventTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3054: The type &apos;{0}&apos; cannot have a Name attribute. Value types and types without a default constructor can be used as items within a ResourceDictionary. 的本地化字符串。
    /// </summary>
    internal static string ParserNoNameOnType
    {
        get
        {
            return ResourceManager.GetString("ParserNoNameOnType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3056: No NamespaceURI is defined for the object &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserNoNamespace
    {
        get
        {
            return ResourceManager.GetString("ParserNoNamespace", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3093: Cannot set Name attribute value &apos;{0}&apos; on element &apos;{1}&apos;. &apos;{1}&apos; is under the scope of element &apos;{2}&apos;, which already had a name registered when it was defined in another scope. 的本地化字符串。
    /// </summary>
    internal static string ParserNoNameUnderDefinitionScopeType
    {
        get
        {
            return ResourceManager.GetString("ParserNoNameUnderDefinitionScopeType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3084: Cannot nest XML data islands. 的本地化字符串。
    /// </summary>
    internal static string ParserNoNestedXmlDataIslands
    {
        get
        {
            return ResourceManager.GetString("ParserNoNestedXmlDataIslands", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3057: Cannot set properties on property elements. 的本地化字符串。
    /// </summary>
    internal static string ParserNoPropOnComplexProp
    {
        get
        {
            return ResourceManager.GetString("ParserNoPropOnComplexProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3058: Cannot find a custom serializer for &apos;{0}&apos; so it cannot be parsed. 的本地化字符串。
    /// </summary>
    internal static string ParserNoSerializer
    {
        get
        {
            return ResourceManager.GetString("ParserNoSerializer", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3059: Style setters do not support child elements. A tag of type &apos;{0}&apos; is not allowed. 的本地化字符串。
    /// </summary>
    internal static string ParserNoSetterChild
    {
        get
        {
            return ResourceManager.GetString("ParserNoSetterChild", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3048: &apos;{0}&apos; value is not a valid MarkupExtension expression. Cannot resolve &apos;{1}&apos; in namespace &apos;{2}&apos;. &apos;{1}&apos; must be a subclass of MarkupExtension. 的本地化字符串。
    /// </summary>
    internal static string ParserNotMarkupExtension
    {
        get
        {
            return ResourceManager.GetString("ParserNotMarkupExtension", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3050: Cannot find the type &apos;{0}&apos;. Note that type names are case sensitive. 的本地化字符串。
    /// </summary>
    internal static string ParserNoType
    {
        get
        {
            return ResourceManager.GetString("ParserNoType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3062: &apos;{0}&apos; XML namespace prefix does not map to a NamespaceURI, so element &apos;{1}&apos; cannot be resolved. 的本地化字符串。
    /// </summary>
    internal static string ParserPrefixNSElement
    {
        get
        {
            return ResourceManager.GetString("ParserPrefixNSElement", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3100: &apos;{0}&apos; XML namespace prefix does not map to a namespace URI, so cannot resolve property &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserPrefixNSProperty
    {
        get
        {
            return ResourceManager.GetString("ParserPrefixNSProperty", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3063: Property &apos;{0}&apos; does not have a value. 的本地化字符串。
    /// </summary>
    internal static string ParserPropNoValue
    {
        get
        {
            return ResourceManager.GetString("ParserPropNoValue", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3064: Only public or internal classes can be used within markup. &apos;{0}&apos; type is not public or internal. 的本地化字符串。
    /// </summary>
    internal static string ParserPublicType
    {
        get
        {
            return ResourceManager.GetString("ParserPublicType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3065: &apos;{0}&apos; property is read-only and cannot be set from markup. 的本地化字符串。
    /// </summary>
    internal static string ParserReadOnlyProp
    {
        get
        {
            return ResourceManager.GetString("ParserReadOnlyProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3066: The type reference cannot find a public type named &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserResourceKeyType
    {
        get
        {
            return ResourceManager.GetString("ParserResourceKeyType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3096: Token is not valid. 的本地化字符串。
    /// </summary>
    internal static string Parsers_IllegalToken
    {
        get
        {
            return ResourceManager.GetString("Parsers_IllegalToken", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3019: Cannot reference the static member &apos;{0}&apos; on the type &apos;{1}&apos; as it is not accessible. 的本地化字符串。
    /// </summary>
    internal static string ParserStaticMemberNotAllowed
    {
        get
        {
            return ResourceManager.GetString("ParserStaticMemberNotAllowed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3067: SynchronousMode attribute must be on the root tag. 的本地化字符串。
    /// </summary>
    internal static string ParserSyncOnRoot
    {
        get
        {
            return ResourceManager.GetString("ParserSyncOnRoot", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3069: Cannot have both the text &apos;{0}&apos; and the child element &apos;{1}&apos; within a property element. 的本地化字符串。
    /// </summary>
    internal static string ParserTextInComplexProp
    {
        get
        {
            return ResourceManager.GetString("ParserTextInComplexProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3068: Text is not valid under an IDictionary or Array property. 的本地化字符串。
    /// </summary>
    internal static string ParserTextInvalidInArrayOrDictionary
    {
        get
        {
            return ResourceManager.GetString("ParserTextInvalidInArrayOrDictionary", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3070: A single XAML file cannot reference more than 4,096 different assemblies. 的本地化字符串。
    /// </summary>
    internal static string ParserTooManyAssemblies
    {
        get
        {
            return ResourceManager.GetString("ParserTooManyAssemblies", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3095: Close tag must immediately follow TypeConverter initialization string &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserTypeConverterTextNeedsEndElement
    {
        get
        {
            return ResourceManager.GetString("ParserTypeConverterTextNeedsEndElement", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3090: TypeConverter syntax error encountered while processing initialization string &apos;{0}&apos;.  Element attributes are not allowed on objects created via TypeConverter. 的本地化字符串。
    /// </summary>
    internal static string ParserTypeConverterTextUnusable
    {
        get
        {
            return ResourceManager.GetString("ParserTypeConverterTextUnusable", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3071: &apos;{0}&apos; is an undeclared namespace. 的本地化字符串。
    /// </summary>
    internal static string ParserUndeclaredNS
    {
        get
        {
            return ResourceManager.GetString("ParserUndeclaredNS", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3072: The property &apos;{0}&apos; does not exist in XML namespace &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserUnknownAttribute
    {
        get
        {
            return ResourceManager.GetString("ParserUnknownAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3073: The attribute &apos;{0}&apos; does not exist in XML namespace &apos;http://schemas.microsoft.com/winfx/2006/xaml&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserUnknownDefAttribute
    {
        get
        {
            return ResourceManager.GetString("ParserUnknownDefAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3074: The tag &apos;{0}&apos; does not exist in XML namespace &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ParserUnknownTag
    {
        get
        {
            return ResourceManager.GetString("ParserUnknownTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3075: Unrecognized XML node type &apos;{0}&apos; found when determining if the current tag is a property element. 的本地化字符串。
    /// </summary>
    internal static string ParserUnknownXmlType
    {
        get
        {
            return ResourceManager.GetString("ParserUnknownXmlType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3086: Parent element or property &apos;{0}&apos; requires an XML data island. To distinguish an XML island from surrounding XAML, wrap the XML data island in &lt;x:XData&gt; ... &lt;/x:XData&gt;. 的本地化字符串。
    /// </summary>
    internal static string ParserXmlIslandMissing
    {
        get
        {
            return ResourceManager.GetString("ParserXmlIslandMissing", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3085: &apos;{0}&apos; element or property cannot contain an XML data island. 的本地化字符串。
    /// </summary>
    internal static string ParserXmlIslandUnexpected
    {
        get
        {
            return ResourceManager.GetString("ParserXmlIslandUnexpected", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3092: XmlLangProperty attribute must specify a property name. 的本地化字符串。
    /// </summary>
    internal static string ParserXmlLangPropertyValueInvalid
    {
        get
        {
            return ResourceManager.GetString("ParserXmlLangPropertyValueInvalid", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC3077: The class &apos;{0}&apos; does not implement IXmlLineInfo. This is required to get position information for the XAML being parsed. 的本地化字符串。
    /// </summary>
    internal static string ParserXmlReaderNoLineInfo
    {
        get
        {
            return ResourceManager.GetString("ParserXmlReaderNoLineInfo", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Preparing for the markup compilation... 的本地化字符串。
    /// </summary>
    internal static string PreparingCompile
    {
        get
        {
            return ResourceManager.GetString("PreparingCompile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4301: Type name &apos;{0}&apos; does not have the expected format &apos;className, assembly&apos;. 的本地化字符串。
    /// </summary>
    internal static string QualifiedNameHasWrongFormat
    {
        get
        {
            return ResourceManager.GetString("QualifiedNameHasWrongFormat", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Reading Resource file: &apos;{0}&apos;... 的本地化字符串。
    /// </summary>
    internal static string ReadResourceFile
    {
        get
        {
            return ResourceManager.GetString("ReadResourceFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Recompiled XAML file : &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string RecompiledXaml
    {
        get
        {
            return ResourceManager.GetString("RecompiledXaml", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Input: Assembly Reference file: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ReferenceFile
    {
        get
        {
            return ResourceManager.GetString("ReferenceFile", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Resource ID is &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ResourceId
    {
        get
        {
            return ResourceManager.GetString("ResourceId", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Generated .resources file: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string ResourcesGenerated
    {
        get
        {
            return ResourceManager.GetString("ResourcesGenerated", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Generating .resources file: &apos;{0}&apos;... 的本地化字符串。
    /// </summary>
    internal static string ResourcesGenerating
    {
        get
        {
            return ResourceManager.GetString("ResourcesGenerating", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 RG1001: Input resource file &apos;{0}&apos; exceeds maximum size of {1} bytes. 的本地化字符串。
    /// </summary>
    internal static string ResourceTooBig
    {
        get
        {
            return ResourceManager.GetString("ResourceTooBig", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6006: {0}.{1}=&quot;{2}&quot; is not valid. &apos;{1}&apos; must be a RoutedEvent registered with a name that ends with the keyword &quot;Event&quot;. 的本地化字符串。
    /// </summary>
    internal static string RoutedEventNotRegistered
    {
        get
        {
            return ResourceManager.GetString("RoutedEventNotRegistered", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 UM1005: You must pass markup files to the task. 的本地化字符串。
    /// </summary>
    internal static string SourceFileNameNeeded
    {
        get
        {
            return ResourceManager.GetString("SourceFileNameNeeded", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4110: SourceName property cannot be set within Style.Triggers section. 的本地化字符串。
    /// </summary>
    internal static string SourceNameNotSupportedForStyleTriggers
    {
        get
        {
            return ResourceManager.GetString("SourceNameNotSupportedForStyleTriggers", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4001: Style does not support both {0} tags and Style.{1} property tags for a single Style. Use one or the other. 的本地化字符串。
    /// </summary>
    internal static string StyleImpliedAndComplexChildren
    {
        get
        {
            return ResourceManager.GetString("StyleImpliedAndComplexChildren", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4002: The Style property tag &apos;{0}&apos; can only be specified directly under a Style tag. 的本地化字符串。
    /// </summary>
    internal static string StyleKnownTagWrongLocation
    {
        get
        {
            return ResourceManager.GetString("StyleKnownTagWrongLocation", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4005: Cannot find the Style {0} &apos;{1}&apos; on the type &apos;{2}&apos;. 的本地化字符串。
    /// </summary>
    internal static string StyleNoPropOrEvent
    {
        get
        {
            return ResourceManager.GetString("StyleNoPropOrEvent", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4003: Cannot resolve the Style {0} &apos;{1}&apos;. Verify that the owning type is the Style&apos;s TargetType, or use Class.Property syntax to specify the {0}. 的本地化字符串。
    /// </summary>
    internal static string StyleNoTarget
    {
        get
        {
            return ResourceManager.GetString("StyleNoTarget", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4004: Style cannot contain child &apos;{0}&apos;. Style child must be a Setter because it is added to the Setters collection. 的本地化字符串。
    /// </summary>
    internal static string StyleNoTopLevelElement
    {
        get
        {
            return ResourceManager.GetString("StyleNoTopLevelElement", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4006: Tags of type &apos;{0}&apos; are not supported in Style sections. 的本地化字符串。
    /// </summary>
    internal static string StyleTagNotSupported
    {
        get
        {
            return ResourceManager.GetString("StyleTagNotSupported", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4007: The event &apos;{0}&apos; cannot be specified on a Target tag in a Style. Use an EventSetter instead. 的本地化字符串。
    /// </summary>
    internal static string StyleTargetNoEvents
    {
        get
        {
            return ResourceManager.GetString("StyleTargetNoEvents", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4008: The text &apos;{0}&apos; is not allowed at this location within a Style section. 的本地化字符串。
    /// </summary>
    internal static string StyleTextNotSupported
    {
        get
        {
            return ResourceManager.GetString("StyleTextNotSupported", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4009: The property &apos;{0}&apos; cannot be set on Style. 的本地化字符串。
    /// </summary>
    internal static string StyleUnknownProp
    {
        get
        {
            return ResourceManager.GetString("StyleUnknownProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6017: &apos;{0}&apos; cannot be the root of a XAML file because it was defined using XAML. 的本地化字符串。
    /// </summary>
    internal static string SubSubClassingNotAllowed
    {
        get
        {
            return ResourceManager.GetString("SubSubClassingNotAllowed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 BG1004: Target Type &apos;{0}&apos; is not supported by this task. 的本地化字符串。
    /// </summary>
    internal static string TargetIsNotSupported
    {
        get
        {
            return ResourceManager.GetString("TargetIsNotSupported", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4011: TargetName property cannot be set on a Style Setter. 的本地化字符串。
    /// </summary>
    internal static string TargetNameNotSupportedForStyleSetters
    {
        get
        {
            return ResourceManager.GetString("TargetNameNotSupportedForStyleSetters", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Microsoft (R) Build Task &apos;{0}&apos; Version &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string TaskLogo
    {
        get
        {
            return ResourceManager.GetString("TaskLogo", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Copyright (C) Microsoft Corporation 2005. All rights reserved. 的本地化字符串。
    /// </summary>
    internal static string TaskRight
    {
        get
        {
            return ResourceManager.GetString("TaskRight", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4101: The Name &apos;{0}&apos; has already been defined. Names must be unique. 的本地化字符串。
    /// </summary>
    internal static string TemplateDupName
    {
        get
        {
            return ResourceManager.GetString("TemplateDupName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4108: The root of a Template content section cannot contain an element of type &apos;{0}&apos;. Only FrameworkElement and FrameworkContentElement types are valid. 的本地化字符串。
    /// </summary>
    internal static string TemplateInvalidRootElementTag
    {
        get
        {
            return ResourceManager.GetString("TemplateInvalidRootElementTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4103: The template property tag &apos;{0}&apos; can only be specified immediately after a ControlTemplate tag. 的本地化字符串。
    /// </summary>
    internal static string TemplateKnownTagWrongLocation
    {
        get
        {
            return ResourceManager.GetString("TemplateKnownTagWrongLocation", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4107: A template can have only a single root element. &apos;{0}&apos; is not allowed. 的本地化字符串。
    /// </summary>
    internal static string TemplateNoMultipleRoots
    {
        get
        {
            return ResourceManager.GetString("TemplateNoMultipleRoots", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4109: Cannot find the Template Property &apos;{0}&apos; on the type &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string TemplateNoProp
    {
        get
        {
            return ResourceManager.GetString("TemplateNoProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4106: Cannot resolve the Template Property &apos;{0}&apos;. Verify that the owning type is the Style&apos;s TargetType, or use Class.Property syntax to specify the property. 的本地化字符串。
    /// </summary>
    internal static string TemplateNoTarget
    {
        get
        {
            return ResourceManager.GetString("TemplateNoTarget", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4111: Cannot find the Trigger target &apos;{0}&apos;.  (The target must appear before any Setters, Triggers, or Conditions that use it.) 的本地化字符串。
    /// </summary>
    internal static string TemplateNoTriggerTarget
    {
        get
        {
            return ResourceManager.GetString("TemplateNoTriggerTarget", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4102: Tags of type &apos;{0}&apos; are not supported in template sections. 的本地化字符串。
    /// </summary>
    internal static string TemplateTagNotSupported
    {
        get
        {
            return ResourceManager.GetString("TemplateTagNotSupported", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4105: The text &apos;{0}&apos; is not allowed at this location within a template section. 的本地化字符串。
    /// </summary>
    internal static string TemplateTextNotSupported
    {
        get
        {
            return ResourceManager.GetString("TemplateTextNotSupported", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4104: The property &apos;{0}&apos; cannot be set as a property element on template. Only Triggers and Storyboards are allowed as property elements. 的本地化字符串。
    /// </summary>
    internal static string TemplateUnknownProp
    {
        get
        {
            return ResourceManager.GetString("TemplateUnknownProp", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC7004: Empty token encountered while parsing. 的本地化字符串。
    /// </summary>
    internal static string TokenizerHelperEmptyToken
    {
        get
        {
            return ResourceManager.GetString("TokenizerHelperEmptyToken", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC7003: Extra data encountered after token while parsing. 的本地化字符串。
    /// </summary>
    internal static string TokenizerHelperExtraDataEncountered
    {
        get
        {
            return ResourceManager.GetString("TokenizerHelperExtraDataEncountered", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC7002: Missing end quote encountered while parsing token. 的本地化字符串。
    /// </summary>
    internal static string TokenizerHelperMissingEndQuote
    {
        get
        {
            return ResourceManager.GetString("TokenizerHelperMissingEndQuote", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC7001: Premature string termination encountered. 的本地化字符串。
    /// </summary>
    internal static string TokenizerHelperPrematureStringTermination
    {
        get
        {
            return ResourceManager.GetString("TokenizerHelperPrematureStringTermination", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 UM1003: Uid is missing for element &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string UidMissing
    {
        get
        {
            return ResourceManager.GetString("UidMissing", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Unknown build error, &apos;{0}&apos;  的本地化字符串。
    /// </summary>
    internal static string UnknownBuildError
    {
        get
        {
            return ResourceManager.GetString("UnknownBuildError", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6013: {0}:ClassModifier=&quot;{1}&quot; is not valid for the language {2}. 的本地化字符串。
    /// </summary>
    internal static string UnknownClassModifier
    {
        get
        {
            return ResourceManager.GetString("UnknownClassModifier", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6002: Unrecognized tag &apos;{0}:{1}&apos; in namespace &apos;http://schemas.microsoft.com/winfx/2006/xaml&apos;. Note that tag names are case sensitive. 的本地化字符串。
    /// </summary>
    internal static string UnknownDefinitionTag
    {
        get
        {
            return ResourceManager.GetString("UnknownDefinitionTag", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6007: &apos;{1}&apos; is not valid. &apos;{0}&apos; is not an event on &apos;{2}&apos;. 的本地化字符串。
    /// </summary>
    internal static string UnknownEventAttribute
    {
        get
        {
            return ResourceManager.GetString("UnknownEventAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6014: {0}:FieldModifier=&quot;{1}&quot; is not valid for the language {2}. 的本地化字符串。
    /// </summary>
    internal static string UnknownFieldModifier
    {
        get
        {
            return ResourceManager.GetString("UnknownFieldModifier", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6020: {0}:TypeArguments=&apos;{1}&apos; is not valid on the tag &apos;{2}&apos;. Either &apos;{2}&apos; is not a generic type or the number of Type arguments in the attribute is wrong. Remove the {0}:TypeArguments attribute because it is allowed only on generic types, or fix its value to match the arity of the generic type &apos;{2}&apos;. 的本地化字符串。
    /// </summary>
    internal static string UnknownGenericType
    {
        get
        {
            return ResourceManager.GetString("UnknownGenericType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6008: &apos;{0}&apos; is not installed properly on this machine. It must be listed in the &lt;compilers&gt; section of machine.config. 的本地化字符串。
    /// </summary>
    internal static string UnknownLanguage
    {
        get
        {
            return ResourceManager.GetString("UnknownLanguage", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 Unknown path operation attempted. 的本地化字符串。
    /// </summary>
    internal static string UnknownPathOperationType
    {
        get
        {
            return ResourceManager.GetString("UnknownPathOperationType", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 LC1003: Localization comment has no value set for target property: &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string UnmatchedLocComment
    {
        get
        {
            return ResourceManager.GetString("UnmatchedLocComment", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4501: Major and minor version number components cannot be negative. 的本地化字符串。
    /// </summary>
    internal static string VersionNumberComponentNegative
    {
        get
        {
            return ResourceManager.GetString("VersionNumberComponentNegative", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC6000: Project file must include the .NET Framework assembly &apos;{0}&apos; in the reference list. 的本地化字符串。
    /// </summary>
    internal static string WinFXAssemblyMissing
    {
        get
        {
            return ResourceManager.GetString("WinFXAssemblyMissing", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC1001: The LocalizationDirectivesToLocFile property value is not valid and must be changed to None, CommentsOnly, or All for the MarkupCompilePass1 task. 的本地化字符串。
    /// </summary>
    internal static string WrongLocalizationPropertySetting_Pass1
    {
        get
        {
            return ResourceManager.GetString("WrongLocalizationPropertySetting_Pass1", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 BG1003: The project file contains a property value that is not valid. 的本地化字符串。
    /// </summary>
    internal static string WrongPropertySetting
    {
        get
        {
            return ResourceManager.GetString("WrongPropertySetting", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4602: Choice cannot follow a Fallback. 的本地化字符串。
    /// </summary>
    internal static string XCRChoiceAfterFallback
    {
        get
        {
            return ResourceManager.GetString("XCRChoiceAfterFallback", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4606: AlternateContent must contain one or more Choice elements. 的本地化字符串。
    /// </summary>
    internal static string XCRChoiceNotFound
    {
        get
        {
            return ResourceManager.GetString("XCRChoiceNotFound", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4601: Choice valid only in AlternateContent. 的本地化字符串。
    /// </summary>
    internal static string XCRChoiceOnlyInAC
    {
        get
        {
            return ResourceManager.GetString("XCRChoiceOnlyInAC", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4640: Namespace &apos;{0}&apos; is marked as compatible with itself using XmlnsCompatibilityAttribute. A namespace cannot directly or indirectly override itself. 的本地化字符串。
    /// </summary>
    internal static string XCRCompatCycle
    {
        get
        {
            return ResourceManager.GetString("XCRCompatCycle", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4631: Duplicate Preserve declaration for element &apos;{1}&apos; in namespace &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRDuplicatePreserve
    {
        get
        {
            return ResourceManager.GetString("XCRDuplicatePreserve", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4617: Duplicate ProcessContent declaration for element &apos;{1}&apos; in namespace &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRDuplicateProcessContent
    {
        get
        {
            return ResourceManager.GetString("XCRDuplicateProcessContent", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4633: Duplicate wildcard Preserve declaration for namespace &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRDuplicateWildcardPreserve
    {
        get
        {
            return ResourceManager.GetString("XCRDuplicateWildcardPreserve", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4619: Duplicate wildcard ProcessContent declaration for namespace &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRDuplicateWildcardProcessContent
    {
        get
        {
            return ResourceManager.GetString("XCRDuplicateWildcardProcessContent", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4605: Fallback valid only in AlternateContent. 的本地化字符串。
    /// </summary>
    internal static string XCRFallbackOnlyInAC
    {
        get
        {
            return ResourceManager.GetString("XCRFallbackOnlyInAC", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4610: &apos;{0}&apos; element is not a valid child of AlternateContent. Only Choice and Fallback elements are valid children of an AlternateContent element. 的本地化字符串。
    /// </summary>
    internal static string XCRInvalidACChild
    {
        get
        {
            return ResourceManager.GetString("XCRInvalidACChild", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4608: &apos;{0}&apos; attribute is not valid for element &apos;{1}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRInvalidAttribInElement
    {
        get
        {
            return ResourceManager.GetString("XCRInvalidAttribInElement", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4611: &apos;{0}&apos; format is not valid. 的本地化字符串。
    /// </summary>
    internal static string XCRInvalidFormat
    {
        get
        {
            return ResourceManager.GetString("XCRInvalidFormat", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4632: Cannot have both a specific and a wildcard Preserve declaration for namespace &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRInvalidPreserve
    {
        get
        {
            return ResourceManager.GetString("XCRInvalidPreserve", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4618: Cannot have both a specific and a wildcard ProcessContent declaration for namespace &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRInvalidProcessContent
    {
        get
        {
            return ResourceManager.GetString("XCRInvalidProcessContent", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4604: Requires attribute must contain a valid namespace prefix. 的本地化字符串。
    /// </summary>
    internal static string XCRInvalidRequiresAttribute
    {
        get
        {
            return ResourceManager.GetString("XCRInvalidRequiresAttribute", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4634: &apos;{0}&apos; attribute value is not a valid XML name. 的本地化字符串。
    /// </summary>
    internal static string XCRInvalidXMLName
    {
        get
        {
            return ResourceManager.GetString("XCRInvalidXMLName", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4607: AlternateContent must contain only one Fallback element. 的本地化字符串。
    /// </summary>
    internal static string XCRMultipleFallbackFound
    {
        get
        {
            return ResourceManager.GetString("XCRMultipleFallbackFound", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4626: MustUnderstand condition failed on namespace &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRMustUnderstandFailed
    {
        get
        {
            return ResourceManager.GetString("XCRMustUnderstandFailed", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4630: Namespace &apos;{0}&apos; has items declared to be preserved but is not declared ignorable. 的本地化字符串。
    /// </summary>
    internal static string XCRNSPreserveNotIgnorable
    {
        get
        {
            return ResourceManager.GetString("XCRNSPreserveNotIgnorable", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4615: &apos;{0}&apos; namespace is declared ProcessContent but is not declared Ignorable. 的本地化字符串。
    /// </summary>
    internal static string XCRNSProcessContentNotIgnorable
    {
        get
        {
            return ResourceManager.GetString("XCRNSProcessContentNotIgnorable", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4603: Choice must contain a Requires attribute. 的本地化字符串。
    /// </summary>
    internal static string XCRRequiresAttribNotFound
    {
        get
        {
            return ResourceManager.GetString("XCRRequiresAttribNotFound", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4612: &apos;{0}&apos; prefix is not defined. 的本地化字符串。
    /// </summary>
    internal static string XCRUndefinedPrefix
    {
        get
        {
            return ResourceManager.GetString("XCRUndefinedPrefix", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4614: Unrecognized compatibility attribute &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRUnknownCompatAttrib
    {
        get
        {
            return ResourceManager.GetString("XCRUnknownCompatAttrib", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4609: Unrecognized compatibility element &apos;{0}&apos;. 的本地化字符串。
    /// </summary>
    internal static string XCRUnknownCompatElement
    {
        get
        {
            return ResourceManager.GetString("XCRUnknownCompatElement", resourceCulture);
        }
    }

    /// <summary>
    ///   查找类似 MC4502: The feature ID string cannot have length 0. 的本地化字符串。
    /// </summary>
    internal static string ZeroLengthFeatureID
    {
        get
        {
            return ResourceManager.GetString("ZeroLengthFeatureID", resourceCulture);
        }
    }


}

public class ReferenceEqualityComparer:IEqualityComparer<object>
{
    public static ReferenceEqualityComparer Instance{ get; }
    public bool Equals(object x, object y)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode(object obj)
    {
        throw new NotImplementedException();
    }
}
