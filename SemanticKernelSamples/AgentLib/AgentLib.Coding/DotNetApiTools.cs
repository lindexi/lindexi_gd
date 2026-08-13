using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;
using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class DotNetApiTools
{
    private const int PageSize = 50;
    private const int SummaryMaxLength = 100;
    private readonly string _workspacePath;

    internal DotNetApiTools(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("代码工作区路径不能为空。", nameof(workspacePath));
        }

        _workspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(_workspacePath))
        {
            throw new DirectoryNotFoundException("指定的代码工作区不存在。");
        }
    }

    internal IReadOnlyList<AITool> AsAITools() =>
        AsToolRegistrations().Select(registration => registration.Tool).ToArray();

    internal IReadOnlyList<ToolRegistration> AsToolRegistrations() =>
    [
        new
        (
            AIFunctionFactory.Create(ListDotNetApiAsync, "ListDotNetApi"),
            arguments => ToolCallPresentationFactory.ForQuery(arguments, "source")
        ),
        new
        (
            AIFunctionFactory.Create(GetDotNetTypeApiAsync, "GetDotNetTypeApi"),
            arguments => ToolCallPresentationFactory.ForQuery(arguments, "typeName")
        ),
    ];

    [Description("列出指定 NuGet 包或 DLL 中的公开类型清单。")]
    internal Task<string> ListDotNetApiAsync
    (
        [Description("NuGet 包及精确版本，格式为“包名/版本号”，例如 Newtonsoft.Json/13.0.3；或 DLL 文件路径。")]
        string source,
        [Description("可选，按类型名关键字过滤。")] string? keyword = null,
        [Description("可选，页码，从 1 开始；不传表示第 1 页。")]
        int? page = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        SourceResolution resolution = ResolveSource(source);
        if (resolution.Error is not null)
        {
            return Task.FromResult(resolution.Error);
        }

        int requestedPage = page ?? 1;
        if (requestedPage < 1)
        {
            return Task.FromResult("页码必须是从 1 开始的正整数。");
        }

        try
        {
            IReadOnlyList<ApiType> allTypes = ReadPublicTypes(resolution.AssemblyPaths!, cancellationToken);
            IReadOnlyList<ApiType> filteredTypes = string.IsNullOrWhiteSpace(keyword)
                ? allTypes
                : allTypes.Where(type => type.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToArray();
            int pageCount = Math.Max(1, (filteredTypes.Count + PageSize - 1) / PageSize);
            if (requestedPage > pageCount)
            {
                return Task.FromResult($"页码 {requestedPage} 超出范围：该查询结果共 {pageCount} 页，请输入 1 到 {pageCount} 之间的页码。");
            }

            IReadOnlyList<ApiType> pageTypes = filteredTypes
                .Skip((requestedPage - 1) * PageSize)
                .Take(PageSize)
                .ToArray();
            return Task.FromResult
            (
                FormatTypeList
                (
                    resolution.DisplayName!, allTypes.Count, filteredTypes.Count, keyword,
                    requestedPage, pageCount, pageTypes
                )
            );
        }
        catch (BadImageFormatException)
        {
            return Task.FromResult($"无法读取“{source}”的 .NET 程序集元数据。");
        }
        catch (IOException ex)
        {
            return Task.FromResult($"读取“{source}”失败：{ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult($"读取“{source}”失败：{ex.Message}");
        }
    }

    [Description("获取指定 NuGet 包或 DLL 中一个类型的完整公开 API。")]
    internal Task<string> GetDotNetTypeApiAsync
    (
        [Description("NuGet 包及精确版本，格式为“包名/版本号”；或 DLL 文件路径。")]
        string source,
        [Description("完整类型名，例如 Xxxx1.CameraCapture。")]
        string typeName,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return Task.FromResult("完整类型名不能为空。");
        }

        SourceResolution resolution = ResolveSource(source);
        if (resolution.Error is not null)
        {
            return Task.FromResult(resolution.Error);
        }

        try
        {
            foreach (string assemblyPath in resolution.AssemblyPaths!)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? result = TryFormatType(assemblyPath, typeName, cancellationToken);
                if (result is not null)
                {
                    return Task.FromResult(result);
                }
            }

            return Task.FromResult($"程序集中未找到公开类型“{typeName}”。");
        }
        catch (BadImageFormatException)
        {
            return Task.FromResult($"无法读取“{source}”的 .NET 程序集元数据。");
        }
        catch (IOException ex)
        {
            return Task.FromResult($"读取“{source}”失败：{ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult($"读取“{source}”失败：{ex.Message}");
        }
    }

    private SourceResolution ResolveSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return SourceResolution.Failed("NuGet 包或 DLL 来源不能为空。");
        }

        source = source.Trim();
        if (source.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            string path = Path.GetFullPath(Path.IsPathRooted(source) ? source : Path.Join(_workspacePath, source));
            return File.Exists(path)
                ? SourceResolution.Succeeded($"DLL {source}", [path])
                : SourceResolution.Failed($"不存在 DLL 文件“{source}”。");
        }

        int separatorIndex = source.LastIndexOf('/');
        if (separatorIndex <= 0 || separatorIndex == source.Length - 1)
        {
            return SourceResolution.Failed("NuGet 查询必须指定精确版本，格式为“包名/版本号”。");
        }

        string packageId = source[..separatorIndex].Trim();
        string version = source[(separatorIndex + 1)..].Trim();
        if (packageId.Length == 0 || version.Length == 0 || packageId.Contains('/') || packageId.Contains('\\'))
        {
            return SourceResolution.Failed("NuGet 查询必须指定精确版本，格式为“包名/版本号”。");
        }

        string packageRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                             ?? Path.Join
                             (
                                 Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget",
                                 "packages"
                             );
        string packagePath = Path.Join(packageRoot, packageId.ToLowerInvariant(), version);
        if (!Directory.Exists(packagePath))
        {
            return SourceResolution.Failed($"本地 NuGet 缓存中不存在包“{packageId}”版本“{version}”。");
        }

        string refPath = Path.Join(packagePath, "ref");
        string libPath = Path.Join(packagePath, "lib");
        string assemblyRoot = Directory.Exists(refPath) ? refPath : libPath;
        string[] assemblyPaths = Directory.Exists(assemblyRoot)
            ? Directory.GetFiles(assemblyRoot, "*.dll", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
        if (assemblyPaths.Length == 0)
        {
            return SourceResolution.Failed($"NuGet 包“{packageId}”版本“{version}”的 ref 或 lib 目录中没有 DLL 文件。");
        }

        return SourceResolution.Succeeded($"包 {packageId}/{version}", assemblyPaths);
    }

    private static IReadOnlyList<ApiType> ReadPublicTypes
    (
        IReadOnlyList<string> assemblyPaths,
        CancellationToken cancellationToken
    )
    {
        var types = new Dictionary<string, ApiType>(StringComparer.Ordinal);
        foreach (string assemblyPath in assemblyPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using FileStream stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
            {
                continue;
            }

            MetadataReader reader = peReader.GetMetadataReader();
            XmlDocumentation documentation = XmlDocumentation.Load(Path.ChangeExtension(assemblyPath, ".xml"));
            foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
            {
                TypeDefinition definition = reader.GetTypeDefinition(handle);
                if (!IsPublicType(reader, handle, definition) ||
                    IsCompilerGenerated(reader, definition.GetCustomAttributes()))
                {
                    continue;
                }

                string fullName = GetFullTypeName(reader, handle);
                if (!types.ContainsKey(fullName))
                {
                    types.Add
                    (
                        fullName,
                        new ApiType
                        (
                            fullName, GetTypeKind(reader, definition),
                            documentation.GetSummary("T:" + fullName)
                        )
                    );
                }
            }
        }

        return types.Values.OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
    }

    private static string FormatTypeList
    (
        string sourceDisplayName,
        int totalCount,
        int filteredCount,
        string? keyword,
        int page,
        int pageCount,
        IReadOnlyList<ApiType> types
    )
    {
        var builder = new StringBuilder();
        builder.Append(sourceDisplayName).Append(" 共 ").Append(totalCount).Append(" 个公开类型");
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            builder.Append("（已按关键字“").Append(keyword).Append("”过滤为 ").Append(filteredCount).Append(" 个）");
        }

        builder.AppendLine("，");
        builder.Append("当前为第 ").Append(page).Append(" 页，每页 ").Append(PageSize).Append(" 个，共 ").Append(pageCount)
            .AppendLine(" 页。");

        if (types.Count == 0)
        {
            builder.AppendLine().AppendLine("没有符合条件的公开类型。");
            return builder.ToString().TrimEnd();
        }

        builder.AppendLine();
        for (int index = 0; index < types.Count; index++)
        {
            ApiType type = types[index];
            int sequence = ((page - 1) * PageSize) + index + 1;
            builder.Append(sequence).Append(". ").Append(type.FullName).Append("（").Append(type.Kind).AppendLine("）");
            if (!string.IsNullOrWhiteSpace(type.Summary))
            {
                builder.Append("   摘要：").AppendLine(Truncate(type.Summary, SummaryMaxLength));
            }
        }

        if (page < pageCount)
        {
            builder.AppendLine().Append("（查看下一页请传入 page: ").Append(page + 1).AppendLine("。）");
        }

        return builder.ToString().TrimEnd();
    }

    private static string? TryFormatType
    (
        string assemblyPath, string requestedTypeName,
        CancellationToken cancellationToken
    )
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
        {
            return null;
        }

        MetadataReader reader = peReader.GetMetadataReader();
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TypeDefinition definition = reader.GetTypeDefinition(handle);
            if (!IsPublicType(reader, handle, definition) ||
                IsCompilerGenerated(reader, definition.GetCustomAttributes()))
            {
                continue;
            }

            string fullName = GetFullTypeName(reader, handle);
            string normalizedRequestedTypeName = requestedTypeName.Replace('+', '.');
            if (!string.Equals(fullName, normalizedRequestedTypeName, StringComparison.Ordinal))
            {
                continue;
            }

            return FormatType
            (
                reader, handle, definition, fullName,
                XmlDocumentation.Load(Path.ChangeExtension(assemblyPath, ".xml"))
            );
        }

        return null;
    }

    private static string FormatType
    (
        MetadataReader reader,
        TypeDefinitionHandle handle,
        TypeDefinition definition,
        string fullName,
        XmlDocumentation documentation
    )
    {
        GenericContext context = GenericContext.ForType(reader, definition);
        var provider = new CSharpSignatureTypeProvider();
        var builder = new StringBuilder();
        string typeDocumentationId = "T:" + fullName;
        string? summary = documentation.GetSummary(typeDocumentationId);
        string? obsolete = GetObsoleteText(reader, definition.GetCustomAttributes(), provider);

        builder.Append(GetTypeDeclaration(reader, handle, definition, fullName, provider, context)).AppendLine();
        AppendObsolete(builder, obsolete);
        AppendDocumentation(builder, summary, null, null);

        var constructors = new List<string>();
        var methods = new List<string>();
        foreach (MethodDefinitionHandle methodHandle in definition.GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(methodHandle);
            if ((method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
            {
                continue;
            }

            string name = reader.GetString(method.Name);
            if (name == ".cctor" || IsAccessorMethod(name))
            {
                continue;
            }

            string formatted = FormatMethod(reader, methodHandle, method, fullName, provider, context, documentation);
            if (name == ".ctor")
            {
                constructors.Add(formatted);
            }
            else if (name != ".cctor")
            {
                methods.Add(formatted);
            }
        }

        AppendSection(builder, "构造函数", constructors);
        AppendSection
        (
            builder, "属性",
            definition.GetProperties()
                .Select
                (propertyHandle =>
                    FormatProperty(reader, propertyHandle, fullName, provider, context, documentation)
                )
                .Where(value => value is not null)!
        );
        AppendSection
        (
            builder, "字段",
            definition.GetFields()
                .Select(fieldHandle => FormatField(reader, fieldHandle, fullName, provider, context, documentation))
                .Where(value => value is not null)!
        );
        AppendSection
        (
            builder, "事件",
            definition.GetEvents()
                .Select(eventHandle => FormatEvent(reader, eventHandle, fullName, provider, context, documentation))
                .Where(value => value is not null)!
        );
        AppendSection(builder, "方法", methods);
        return builder.ToString().TrimEnd();
    }

    private static string GetTypeDeclaration
    (
        MetadataReader reader,
        TypeDefinitionHandle handle,
        TypeDefinition definition,
        string fullName,
        CSharpSignatureTypeProvider provider,
        GenericContext context
    )
    {
        string kind = GetTypeKind(reader, definition);
        string keyword = kind switch
        {
            "接口" => "interface",
            "枚举" => "enum",
            "结构" => "struct",
            "委托" => "delegate",
            _ => "class",
        };
        string modifiers = (definition.Attributes & TypeAttributes.Abstract) != 0 &&
                           (definition.Attributes & TypeAttributes.Sealed) != 0
            ? "static "
            : (definition.Attributes & TypeAttributes.Abstract) != 0 && kind == "类"
                ? "abstract "
                : (definition.Attributes & TypeAttributes.Sealed) != 0 && kind == "类"
                    ? "sealed "
                    : string.Empty;
        string typeName = context.TypeParameters.Length == 0
            ? fullName
            : fullName + "<" + string.Join(", ", context.TypeParameters) + ">";
        var bases = new List<string>();
        if (!definition.BaseType.IsNil && kind == "类")
        {
            string baseType = provider.GetTypeFromHandle(reader, definition.BaseType, context);
            if (baseType is not "object" and not "System.Object")
            {
                bases.Add(baseType);
            }
        }

        bases.AddRange
        (
            definition.GetInterfaceImplementations().Select
            (interfaceHandle =>
                {
                    InterfaceImplementation implementation = reader.GetInterfaceImplementation(interfaceHandle);
                    return provider.GetTypeFromHandle(reader, implementation.Interface, context);
                }
            )
        );
        string inheritance = bases.Count == 0
            ? string.Empty
            : " : " + string.Join(", ", bases.Distinct(StringComparer.Ordinal));
        string constraints = FormatGenericConstraints(reader, definition.GetGenericParameters(), provider, context);
        return $"public {modifiers}{keyword} {typeName}{inheritance}{constraints}";
    }

    private static string FormatMethod
    (
        MetadataReader reader,
        MethodDefinitionHandle handle,
        MethodDefinition method,
        string declaringTypeName,
        CSharpSignatureTypeProvider provider,
        GenericContext typeContext,
        XmlDocumentation documentation
    )
    {
        GenericContext context = GenericContext.ForMethod(reader, method, typeContext.TypeParameters);
        MethodSignature<string> signature = method.DecodeSignature(provider, context);
        string metadataName = reader.GetString(method.Name);
        string methodName = metadataName == ".ctor"
            ? RemoveGenericArity(declaringTypeName[(declaringTypeName.LastIndexOf('.') + 1)..])
            : FormatMethodName(metadataName);
        if (context.MethodParameters.Length > 0 && metadataName != ".ctor")
        {
            methodName += "<" + string.Join(", ", context.MethodParameters) + ">";
        }

        Dictionary<int, Parameter> parameters = method.GetParameters()
            .Select(reader.GetParameter)
            .Where(parameter => parameter.SequenceNumber > 0)
            .ToDictionary(parameter => parameter.SequenceNumber - 1);
        var formattedParameters = new List<string>();
        for (int index = 0; index < signature.ParameterTypes.Length; index++)
        {
            parameters.TryGetValue(index, out Parameter parameter);
            string parameterName = parameter.Name.IsNil ? $"arg{index}" : reader.GetString(parameter.Name);
            string parameterType = ApplyGenericNames(signature.ParameterTypes[index], context, true);
            string prefix = GetParameterPrefix
            (
                reader, parameter, parameterType,
                index == 0 && IsExtensionMethod(reader, method.GetCustomAttributes())
            );
            if (parameterType.EndsWith("&", StringComparison.Ordinal))
            {
                parameterType = parameterType[..^1];
            }

            string defaultValue = GetDefaultValue(reader, parameter);
            formattedParameters.Add($"{prefix}{parameterType} {parameterName}{defaultValue}");
        }

        string modifiers = GetMethodModifiers(method);
        string returnType = ApplyGenericNames(signature.ReturnType, context, true);
        string declaration = metadataName == ".ctor"
            ? $"public {methodName}({string.Join(", ", formattedParameters)})"
            : $"public {modifiers}{returnType} {methodName}({string.Join(", ", formattedParameters)}){FormatGenericConstraints(reader, method.GetGenericParameters(), provider, context)}";
        string documentationId =
            BuildMethodDocumentationId(reader, declaringTypeName, metadataName, signature, context);
        return FormatMemberBlock
        (
            reader, declaration, method.GetCustomAttributes(), provider, documentation,
            documentationId
        );
    }

    private static string? FormatProperty
    (
        MetadataReader reader,
        PropertyDefinitionHandle handle,
        string declaringTypeName,
        CSharpSignatureTypeProvider provider,
        GenericContext context,
        XmlDocumentation documentation
    )
    {
        PropertyDefinition property = reader.GetPropertyDefinition(handle);
        PropertyAccessors accessors = property.GetAccessors();
        MethodDefinition? getter = GetPublicAccessor(reader, accessors.Getter);
        MethodDefinition? setter = GetPublicAccessor(reader, accessors.Setter);
        if (getter is null && setter is null)
        {
            return null;
        }

        MethodSignature<string> signature = property.DecodeSignature(provider, context);
        string name = reader.GetString(property.Name);
        string propertyType = ApplyGenericNames(signature.ReturnType, context, true);
        string accessorText =
            $"{{{(getter is not null ? " get;" : string.Empty)}{(setter is not null ? " set;" : string.Empty)} }}";
        string modifiers = getter is not null && (getter.Value.Attributes & MethodAttributes.Static) != 0
                           || setter is not null && (setter.Value.Attributes & MethodAttributes.Static) != 0
            ? "static "
            : string.Empty;
        string declaration;
        if (signature.ParameterTypes.Length > 0)
        {
            string parameters = string.Join
            (
                ", ",
                signature.ParameterTypes.Select
                ((type, index) =>
                    $"{ApplyGenericNames(type, context, true)} arg{index}"
                )
            );
            declaration = $"public {modifiers}{propertyType} this[{parameters}] {accessorText}";
        }
        else
        {
            declaration = $"public {modifiers}{propertyType} {name} {accessorText}";
        }

        string documentationId = "P:" + declaringTypeName + "." + name;
        return FormatMemberBlock
        (
            reader, declaration, property.GetCustomAttributes(), provider, documentation,
            documentationId
        );
    }

    private static string? FormatField
    (
        MetadataReader reader,
        FieldDefinitionHandle handle,
        string declaringTypeName,
        CSharpSignatureTypeProvider provider,
        GenericContext context,
        XmlDocumentation documentation
    )
    {
        FieldDefinition field = reader.GetFieldDefinition(handle);
        if ((field.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public
            || IsCompilerGenerated(reader, field.GetCustomAttributes()))
        {
            return null;
        }

        string fieldType = ApplyGenericNames(field.DecodeSignature(provider, context), context, true);
        string name = reader.GetString(field.Name);
        string modifiers = (field.Attributes & FieldAttributes.Literal) != 0
            ? "const "
            : (field.Attributes & FieldAttributes.Static) != 0 && (field.Attributes & FieldAttributes.InitOnly) != 0
                ? "static readonly "
                : (field.Attributes & FieldAttributes.Static) != 0
                    ? "static "
                    : (field.Attributes & FieldAttributes.InitOnly) != 0
                        ? "readonly "
                        : string.Empty;
        string value = field.GetDefaultValue().IsNil
            ? string.Empty
            : " = " + FormatConstant(reader, reader.GetConstant(field.GetDefaultValue()));
        string declaration = $"public {modifiers}{fieldType} {name}{value}";
        return FormatMemberBlock
        (
            reader, declaration, field.GetCustomAttributes(), provider, documentation,
            "F:" + declaringTypeName + "." + name
        );
    }

    private static string? FormatEvent
    (
        MetadataReader reader,
        EventDefinitionHandle handle,
        string declaringTypeName,
        CSharpSignatureTypeProvider provider,
        GenericContext context,
        XmlDocumentation documentation
    )
    {
        EventDefinition eventDefinition = reader.GetEventDefinition(handle);
        EventAccessors accessors = eventDefinition.GetAccessors();
        MethodDefinition? adder = GetPublicAccessor(reader, accessors.Adder);
        MethodDefinition? remover = GetPublicAccessor(reader, accessors.Remover);
        if (adder is null && remover is null)
        {
            return null;
        }

        string name = reader.GetString(eventDefinition.Name);
        string eventType = provider.GetTypeFromHandle(reader, eventDefinition.Type, context);
        string modifiers = adder is not null && (adder.Value.Attributes & MethodAttributes.Static) != 0
            ? "static "
            : string.Empty;
        string declaration = $"public {modifiers}event {eventType} {name}";
        return FormatMemberBlock
        (
            reader, declaration, eventDefinition.GetCustomAttributes(), provider, documentation,
            "E:" + declaringTypeName + "." + name
        );
    }

    private static string FormatMemberBlock
    (
        MetadataReader reader,
        string declaration,
        CustomAttributeHandleCollection attributes,
        CSharpSignatureTypeProvider provider,
        XmlDocumentation documentation,
        string documentationId
    )
    {
        var builder = new StringBuilder(declaration);
        AppendObsolete(builder, GetObsoleteText(reader, attributes, provider));
        AppendDocumentation
        (
            builder, documentation.GetSummary(documentationId),
            documentation.GetParameters(documentationId), documentation.GetReturns(documentationId)
        );
        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string title, IEnumerable<string?> members)
    {
        string[] values = members.Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray();
        if (values.Length == 0)
        {
            return;
        }

        builder.AppendLine().AppendLine(title + "：");
        foreach (string value in values)
        {
            foreach (string line in value.Split('\n'))
            {
                builder.Append("  ").AppendLine(line.TrimEnd('\r'));
            }
        }
    }

    private static void AppendObsolete(StringBuilder builder, string? obsolete)
    {
        if (obsolete is not null)
        {
            builder.AppendLine().Append("[Obsolete");
            if (obsolete.Length > 0)
            {
                builder.Append("(\"").Append(obsolete.Replace("\"", "\\\"", StringComparison.Ordinal)).Append("\")");
            }

            builder.Append(']');
        }
    }

    private static void AppendDocumentation
    (
        StringBuilder builder, string? summary,
        IReadOnlyDictionary<string, string>? parameters, string? returns
    )
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            builder.AppendLine().Append("摘要：").Append(summary);
        }

        if (parameters is not null)
        {
            foreach ((string name, string text) in parameters)
            {
                builder.AppendLine().Append("参数 ").Append(name).Append("：").Append(text);
            }
        }

        if (!string.IsNullOrWhiteSpace(returns))
        {
            builder.AppendLine().Append("返回：").Append(returns);
        }
    }

    private static MethodDefinition? GetPublicAccessor(MetadataReader reader, MethodDefinitionHandle handle)
    {
        if (handle.IsNil)
        {
            return null;
        }

        MethodDefinition method = reader.GetMethodDefinition(handle);
        return (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public ? method : null;
    }

    private static string GetMethodModifiers(MethodDefinition method)
    {
        var modifiers = new List<string>();
        if ((method.Attributes & MethodAttributes.Static) != 0)
        {
            modifiers.Add("static");
        }

        if ((method.Attributes & MethodAttributes.Abstract) != 0)
        {
            modifiers.Add("abstract");
        }
        else if ((method.Attributes & MethodAttributes.Virtual) != 0 &&
                 (method.Attributes & MethodAttributes.NewSlot) != 0)
        {
            modifiers.Add("virtual");
        }

        return modifiers.Count == 0 ? string.Empty : string.Join(" ", modifiers) + " ";
    }

    private static string GetParameterPrefix
    (
        MetadataReader reader, Parameter parameter, string type,
        bool isExtensionReceiver
    )
    {
        if (isExtensionReceiver)
        {
            return "this ";
        }

        if (IsAttribute(reader, parameter.GetCustomAttributes(), "System.ParamArrayAttribute"))
        {
            return "params ";
        }

        if (type.EndsWith("&", StringComparison.Ordinal))
        {
            if ((parameter.Attributes & ParameterAttributes.Out) != 0)
            {
                return "out ";
            }

            if ((parameter.Attributes & ParameterAttributes.In) != 0)
            {
                return "in ";
            }

            return "ref ";
        }

        return string.Empty;
    }

    private static string GetDefaultValue(MetadataReader reader, Parameter parameter)
    {
        ConstantHandle constantHandle = parameter.GetDefaultValue();
        return constantHandle.IsNil ? string.Empty : " = " + FormatConstant(reader, reader.GetConstant(constantHandle));
    }

    private static string FormatConstant(MetadataReader reader, Constant constant)
    {
        BlobReader blob = reader.GetBlobReader(constant.Value);
        object? value = constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => blob.ReadBoolean(),
            ConstantTypeCode.Char => (char)blob.ReadUInt16(),
            ConstantTypeCode.SByte => blob.ReadSByte(),
            ConstantTypeCode.Byte => blob.ReadByte(),
            ConstantTypeCode.Int16 => blob.ReadInt16(),
            ConstantTypeCode.UInt16 => blob.ReadUInt16(),
            ConstantTypeCode.Int32 => blob.ReadInt32(),
            ConstantTypeCode.UInt32 => blob.ReadUInt32(),
            ConstantTypeCode.Int64 => blob.ReadInt64(),
            ConstantTypeCode.UInt64 => blob.ReadUInt64(),
            ConstantTypeCode.Single => blob.ReadSingle(),
            ConstantTypeCode.Double => blob.ReadDouble(),
            ConstantTypeCode.String => blob.ReadUTF16(blob.Length),
            ConstantTypeCode.NullReference => null,
            _ => null,
        };
        return value switch
        {
            null => "null",
            bool boolean => boolean ? "true" : "false",
            char character => $"'{character}'",
            string text => $"\"{text.Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            float number => number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f",
            double number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null",
        };
    }

    private static string FormatGenericConstraints
    (
        MetadataReader reader,
        GenericParameterHandleCollection handles,
        CSharpSignatureTypeProvider provider,
        GenericContext context
    )
    {
        var constraints = new List<string>();
        foreach (GenericParameterHandle handle in handles)
        {
            GenericParameter parameter = reader.GetGenericParameter(handle);
            var values = new List<string>();
            GenericParameterAttributes attributes = parameter.Attributes;
            if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                values.Add("class");
            }

            if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                values.Add("struct");
            }

            values.AddRange
            (
                parameter.GetConstraints().Select
                (constraintHandle =>
                    {
                        GenericParameterConstraint constraint = reader.GetGenericParameterConstraint(constraintHandle);
                        return provider.GetTypeFromHandle(reader, constraint.Type, context);
                    }
                ).Where(value => value != "System.ValueType" && value != "ValueType")
            );
            if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0
                && (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
            {
                values.Add("new()");
            }

            if (values.Count > 0)
            {
                constraints.Add($" where {reader.GetString(parameter.Name)} : {string.Join(", ", values)}");
            }
        }

        return string.Concat(constraints);
    }

    private static string BuildMethodDocumentationId
    (
        MetadataReader reader,
        string declaringTypeName,
        string methodName,
        MethodSignature<string> signature,
        GenericContext context
    )
    {
        string documentationName = methodName switch
        {
            ".ctor" => "#ctor",
            ".cctor" => "#cctor",
            _ => methodName,
        };
        if (context.MethodParameters.Length > 0)
        {
            documentationName += "``" + context.MethodParameters.Length;
        }

        if (signature.ParameterTypes.Length == 0)
        {
            return "M:" + declaringTypeName + "." + documentationName;
        }

        string parameters = string.Join
        (
            ",",
            signature.ParameterTypes.Select(type => ToDocumentationTypeName(ApplyGenericNames(type, context, true)))
        );
        return "M:" + declaringTypeName + "." + documentationName + "(" + parameters + ")";
    }

    private static string ToDocumentationTypeName(string typeName)
    {
        typeName = typeName switch
        {
            "bool" => "System.Boolean",
            "byte" => "System.Byte",
            "char" => "System.Char",
            "double" => "System.Double",
            "short" => "System.Int16",
            "int" => "System.Int32",
            "long" => "System.Int64",
            "nint" => "System.IntPtr",
            "object" => "System.Object",
            "sbyte" => "System.SByte",
            "float" => "System.Single",
            "string" => "System.String",
            "ushort" => "System.UInt16",
            "uint" => "System.UInt32",
            "ulong" => "System.UInt64",
            "nuint" => "System.UIntPtr",
            "void" => "System.Void",
            _ => typeName,
        };
        return typeName
            .Replace("&", "@", StringComparison.Ordinal)
            .Replace("<", "{", StringComparison.Ordinal)
            .Replace(">", "}", StringComparison.Ordinal)
            .Replace("!", "`", StringComparison.Ordinal);
    }

    private static string ApplyGenericNames(string text, GenericContext context, bool includeMethodParameters)
    {
        if (includeMethodParameters)
        {
            for (int index = context.MethodParameters.Length - 1; index >= 0; index--)
            {
                text = text.Replace("!!" + index, context.MethodParameters[index], StringComparison.Ordinal);
            }
        }

        for (int index = context.TypeParameters.Length - 1; index >= 0; index--)
        {
            text = text.Replace("!" + index, context.TypeParameters[index], StringComparison.Ordinal);
        }

        return text;
    }

    private static bool IsAccessorMethod(string methodName) =>
        methodName.StartsWith("get_", StringComparison.Ordinal)
        || methodName.StartsWith("set_", StringComparison.Ordinal)
        || methodName.StartsWith("add_", StringComparison.Ordinal)
        || methodName.StartsWith("remove_", StringComparison.Ordinal);

    private static string FormatMethodName(string metadataName) => metadataName switch
    {
        "op_Addition" => "operator +",
        "op_Subtraction" => "operator -",
        "op_Multiply" => "operator *",
        "op_Division" => "operator /",
        "op_Modulus" => "operator %",
        "op_Equality" => "operator ==",
        "op_Inequality" => "operator !=",
        "op_LessThan" => "operator <",
        "op_GreaterThan" => "operator >",
        "op_LessThanOrEqual" => "operator <=",
        "op_GreaterThanOrEqual" => "operator >=",
        "op_Implicit" => "implicit operator",
        "op_Explicit" => "explicit operator",
        "op_UnaryNegation" => "operator -",
        "op_UnaryPlus" => "operator +",
        "op_Increment" => "operator ++",
        "op_Decrement" => "operator --",
        "op_LogicalNot" => "operator !",
        "op_BitwiseAnd" => "operator &",
        "op_BitwiseOr" => "operator |",
        "op_ExclusiveOr" => "operator ^",
        "op_LeftShift" => "operator <<",
        "op_RightShift" => "operator >>",
        _ => metadataName,
    };

    private static bool IsExtensionMethod(MetadataReader reader, CustomAttributeHandleCollection attributes) =>
        IsAttribute(reader, attributes, "System.Runtime.CompilerServices.ExtensionAttribute");

    private static bool IsCompilerGenerated(MetadataReader reader, CustomAttributeHandleCollection attributes) =>
        IsAttribute(reader, attributes, "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    private static bool IsAttribute
    (
        MetadataReader reader, CustomAttributeHandleCollection attributes,
        string attributeTypeName
    )
    {
        foreach (CustomAttributeHandle handle in attributes)
        {
            if (GetAttributeTypeName(reader, reader.GetCustomAttribute(handle)) == attributeTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetObsoleteText
    (
        MetadataReader reader, CustomAttributeHandleCollection attributes,
        CSharpSignatureTypeProvider provider
    )
    {
        foreach (CustomAttributeHandle handle in attributes)
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            if (GetAttributeTypeName(reader, attribute) != "System.ObsoleteAttribute")
            {
                continue;
            }

            try
            {
                CustomAttributeValue<string> value = attribute.DecodeValue(provider);
                return value.FixedArguments.Length > 0
                    ? value.FixedArguments[0].Value as string ?? string.Empty
                    : string.Empty;
            }
            catch (BadImageFormatException)
            {
                return string.Empty;
            }
        }

        return null;
    }

    private static string? GetAttributeTypeName(MetadataReader reader, CustomAttribute attribute)
    {
        EntityHandle parent = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor)
                .Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor)
                .GetDeclaringType(),
            _ => default,
        };
        return parent.Kind switch
        {
            HandleKind.TypeReference => GetFullTypeName(reader, (TypeReferenceHandle)parent),
            HandleKind.TypeDefinition => GetFullTypeName(reader, (TypeDefinitionHandle)parent),
            _ => null,
        };
    }

    private static bool IsPublicType(MetadataReader reader, TypeDefinitionHandle handle, TypeDefinition definition)
    {
        TypeAttributes visibility = definition.Attributes & TypeAttributes.VisibilityMask;
        if (visibility == TypeAttributes.Public)
        {
            return true;
        }

        if (visibility != TypeAttributes.NestedPublic)
        {
            return false;
        }

        TypeDefinitionHandle declaringType = definition.GetDeclaringType();
        return !declaringType.IsNil && IsPublicType(reader, declaringType, reader.GetTypeDefinition(declaringType));
    }

    private static string GetTypeKind(MetadataReader reader, TypeDefinition definition)
    {
        if ((definition.Attributes & TypeAttributes.Interface) != 0)
        {
            return "接口";
        }

        string baseType = GetEntityTypeFullName(reader, definition.BaseType);
        if (baseType == "System.Enum")
        {
            return "枚举";
        }

        if (baseType == "System.ValueType")
        {
            return "结构";
        }

        if (baseType is "System.MulticastDelegate" or "System.Delegate")
        {
            return "委托";
        }

        return "类";
    }

    private static string GetEntityTypeFullName(MetadataReader reader, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => GetFullTypeName(reader, (TypeDefinitionHandle)handle),
        HandleKind.TypeReference => GetFullTypeName(reader, (TypeReferenceHandle)handle),
        _ => string.Empty,
    };

    private static string GetFullTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        TypeDefinition definition = reader.GetTypeDefinition(handle);
        string name = RemoveGenericArity(reader.GetString(definition.Name));
        TypeDefinitionHandle declaringType = definition.GetDeclaringType();
        if (!declaringType.IsNil)
        {
            return GetFullTypeName(reader, declaringType) + "." + name;
        }

        string ns = reader.GetString(definition.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

    private static string GetFullTypeName(MetadataReader reader, TypeReferenceHandle handle)
    {
        TypeReference reference = reader.GetTypeReference(handle);
        string name = RemoveGenericArity(reader.GetString(reference.Name));
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return GetFullTypeName(reader, (TypeReferenceHandle)reference.ResolutionScope) + "." + name;
        }

        string ns = reader.GetString(reference.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

    private static string RemoveGenericArity(string name)
    {
        int index = name.IndexOf('`');
        return index < 0 ? name : name[..index];
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";

    private sealed record SourceResolution(string? DisplayName, IReadOnlyList<string>? AssemblyPaths, string? Error)
    {
        internal static SourceResolution Succeeded(string displayName, IReadOnlyList<string> assemblyPaths) =>
            new(displayName, assemblyPaths, null);

        internal static SourceResolution Failed(string error) => new(null, null, error);
    }

    private sealed record ApiType(string FullName, string Kind, string? Summary);

    private readonly record struct GenericContext(
        ImmutableArray<string> TypeParameters,
        ImmutableArray<string> MethodParameters)
    {
        internal static GenericContext ForType(MetadataReader reader, TypeDefinition definition) =>
            new
            (
                definition.GetGenericParameters()
                    .Select(handle => reader.GetString(reader.GetGenericParameter(handle).Name)).ToImmutableArray(),
                ImmutableArray<string>.Empty
            );

        internal static GenericContext ForMethod
        (
            MetadataReader reader, MethodDefinition method,
            ImmutableArray<string> typeParameters
        ) =>
            new
            (
                typeParameters,
                method.GetGenericParameters()
                    .Select(handle => reader.GetString(reader.GetGenericParameter(handle).Name)).ToImmutableArray()
            );
    }

    private sealed class CSharpSignatureTypeProvider : ISignatureTypeProvider<string, GenericContext>,
        ICustomAttributeTypeProvider<string>
    {
        public string GetArrayType(string elementType, ArrayShape shape) =>
            elementType + "[" + new string(',', shape.Rank - 1) + "]";

        public string GetByReferenceType(string elementType) => elementType + "&";

        public string GetFunctionPointerType(MethodSignature<string> signature) => "delegate*<" +
            string.Join(", ", signature.ParameterTypes.Append(signature.ReturnType)) + ">";

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
            genericType + "<" + string.Join(", ", typeArguments) + ">";

        public string GetGenericMethodParameter(GenericContext genericContext, int index) => "!!" + index;
        public string GetGenericTypeParameter(GenericContext genericContext, int index) => "!" + index;
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetPinnedType(string elementType) => elementType;
        public string GetPointerType(string elementType) => elementType + "*";

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Boolean => "bool",
            PrimitiveTypeCode.Byte => "byte",
            PrimitiveTypeCode.Char => "char",
            PrimitiveTypeCode.Double => "double",
            PrimitiveTypeCode.Int16 => "short",
            PrimitiveTypeCode.Int32 => "int",
            PrimitiveTypeCode.Int64 => "long",
            PrimitiveTypeCode.IntPtr => "nint",
            PrimitiveTypeCode.Object => "object",
            PrimitiveTypeCode.SByte => "sbyte",
            PrimitiveTypeCode.Single => "float",
            PrimitiveTypeCode.String => "string",
            PrimitiveTypeCode.TypedReference => "TypedReference",
            PrimitiveTypeCode.UInt16 => "ushort",
            PrimitiveTypeCode.UInt32 => "uint",
            PrimitiveTypeCode.UInt64 => "ulong",
            PrimitiveTypeCode.UIntPtr => "nuint",
            PrimitiveTypeCode.Void => "void",
            _ => typeCode.ToString(),
        };

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            GetFullTypeName(reader, handle);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            GetFullTypeName(reader, handle);

        public string GetTypeFromSpecification
        (
            MetadataReader reader, GenericContext genericContext,
            TypeSpecificationHandle handle, byte rawTypeKind
        ) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

        public string GetSystemType() => "System.Type";
        public bool IsSystemType(string type) => type == "System.Type";
        public string GetTypeFromSerializedName(string name) => name;
        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

        internal string GetTypeFromHandle(MetadataReader reader, EntityHandle handle, GenericContext context) =>
            handle.Kind switch
            {
                HandleKind.TypeDefinition => GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0),
                HandleKind.TypeReference => GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
                HandleKind.TypeSpecification => GetTypeFromSpecification
                (
                    reader, context,
                    (TypeSpecificationHandle)handle, 0
                ),
                _ => handle.Kind.ToString(),
            };
    }

    private sealed class XmlDocumentation
    {
        private static readonly XmlDocumentation Empty = new(new Dictionary<string, XElement>(StringComparer.Ordinal));
        private readonly IReadOnlyDictionary<string, XElement> _members;

        private XmlDocumentation(IReadOnlyDictionary<string, XElement> members)
        {
            _members = members;
        }

        internal static XmlDocumentation Load(string path)
        {
            if (!File.Exists(path))
            {
                return Empty;
            }

            try
            {
                XDocument document = XDocument.Load(path, LoadOptions.None);
                Dictionary<string, XElement> members = document.Descendants("member")
                    .Where(element => element.Attribute("name") is not null)
                    .GroupBy(element => (string)element.Attribute("name")!, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                return new XmlDocumentation(members);
            }
            catch (System.Xml.XmlException)
            {
                return Empty;
            }
        }

        internal string? GetSummary(string documentationId) => GetText(documentationId, "summary");
        internal string? GetReturns(string documentationId) => GetText(documentationId, "returns");

        internal IReadOnlyDictionary<string, string>? GetParameters(string documentationId)
        {
            if (!_members.TryGetValue(documentationId, out XElement? member))
            {
                return null;
            }

            Dictionary<string, string> parameters = member.Elements("param")
                .Where(element => element.Attribute("name") is not null)
                .Select(element => new { Name = (string)element.Attribute("name")!, Text = NormalizeText(element) })
                .Where(parameter => parameter.Text is not null)
                .ToDictionary(parameter => parameter.Name, parameter => parameter.Text!, StringComparer.Ordinal);
            return parameters.Count == 0 ? null : parameters;
        }

        private string? GetText(string documentationId, string elementName)
        {
            return _members.TryGetValue(documentationId, out XElement? member)
                ? NormalizeText(member.Element(elementName))
                : null;
        }

        private static string? NormalizeText(XElement? element)
        {
            if (element is null)
            {
                return null;
            }

            string text = string.Join(" ", element.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return text.Length == 0 ? null : text;
        }
    }
}