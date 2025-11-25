using Microsoft.CodeAnalysis;

namespace DotNetCampus.Storage.Analyzer;

static class SaveInfoPropertyTypeInfoConverter
{
    public static SaveInfoPropertyTypeInfo ToTypeInfo(ITypeSymbol propertyTypeSymbol)
    {
        bool isListType = false;
        string? listGenericType = null;
        bool isNullableValueType = false;

        bool isEnumType = false;

        var propertyType = propertyTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (propertyTypeSymbol is INamedTypeSymbol namedType)
        {
            isEnumType = namedType.TypeKind == TypeKind.Enum;

            if (namedType.IsGenericType)
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

                if (typeName is "System.Nullable<>")
                {
                    isNullableValueType = true;

                    var typeArgument = namedType.TypeArguments.FirstOrDefault();
                    if (typeArgument != null)
                    {
                        // 类型应该取可空类型的实际类型
                        propertyType = typeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }
                }
            }
        }

        return new SaveInfoPropertyTypeInfo()
        {
            IsListType = isListType,
            ListGenericType = listGenericType,
            IsEnumType = isEnumType,
            IsNullableValueType = isNullableValueType,
            PropertyType = propertyType,
        };
    }
}