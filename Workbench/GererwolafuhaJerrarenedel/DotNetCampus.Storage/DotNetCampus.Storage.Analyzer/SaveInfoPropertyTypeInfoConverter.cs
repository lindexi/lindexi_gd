using Microsoft.CodeAnalysis;

namespace DotNetCampus.Storage.Analyzer;

static class SaveInfoPropertyTypeInfoConverter
{
    public static SaveInfoPropertyTypeInfo ToTypeInfo(ITypeSymbol propertyTypeSymbol)
    {
        bool isListType = false;
        string? listGenericType = null;
        bool isNullableValueType = false;

        if (propertyTypeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.ConstructUnboundGenericType().ToDisplayString();
            if (typeName is "System.Collections.Generic.List<>"
                or "System.Collections.Generic.IReadOnlyList<>"
                or "System.Collections.Generic.IList<>")
            {
                var typeArgument = namedType.TypeArguments.FirstOrDefault();
                listGenericType = typeArgument?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // 预期这里一定不是空，即 isListType 是 true 值
                isListType = listGenericType != null;
            }
        }

        var isEnumType = IsEnumType(propertyTypeSymbol);

        return new SaveInfoPropertyTypeInfo()
        {
            IsListType = isListType,
            ListGenericType = listGenericType,
            IsEnumType = isEnumType,
            IsNullableValueType = isNullableValueType
        };
    }

    /// <summary>
    /// 判断是否为枚举类型，包括可空枚举类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsEnumType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.TypeKind == TypeKind.Enum)
            {
                return true;
            }
            // 判断是否为可空枚举类型
            if (namedType.IsGenericType && namedType.ConstructUnboundGenericType().ToDisplayString() == "System.Nullable<>")
            {
                var typeArgument = namedType.TypeArguments.FirstOrDefault();
                if (typeArgument != null && typeArgument.TypeKind == TypeKind.Enum)
                {
                    return true;
                }
            }
        }

        return false;
    }
}