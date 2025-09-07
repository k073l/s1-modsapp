namespace ModsApp.Helpers;

public static class TypeNameHelper
{
    public static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // Nullable<T> → T?
            return GetFriendlyTypeName(Nullable.GetUnderlyingType(type)) + "?";
        }

        if (type.IsArray)
        {
            // ElementType[]
            return GetFriendlyTypeName(type.GetElementType()) + "[]";
        }

        if (type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // Tuple<T1, T2> → (T1, T2)
            var tupleArgs = type.GetGenericArguments();
            return "(" + string.Join(", ", tupleArgs.Select(GetFriendlyTypeName)) + ")";
        }

        if (type.IsGenericType)
        {
            var typeName = type.Name;
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex >= 0)
                typeName = typeName[..backtickIndex];

            var args = type.GetGenericArguments();
            return $"{typeName}<{string.Join(", ", args.Select(GetFriendlyTypeName))}>";
        }

        var alias = GetCSharpAlias(type);
        return alias ?? type.Name;
    }

    private static string GetCSharpAlias(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Boolean => "bool",
        TypeCode.Byte => "byte",
        TypeCode.Char => "char",
        TypeCode.Decimal => "decimal",
        TypeCode.Double => "double",
        TypeCode.Int16 => "short",
        TypeCode.Int32 => "int",
        TypeCode.Int64 => "long",
        TypeCode.SByte => "sbyte",
        TypeCode.Single => "float",
        TypeCode.String => "string",
        TypeCode.UInt16 => "ushort",
        TypeCode.UInt32 => "uint",
        TypeCode.UInt64 => "ulong",
        _ when type == typeof(object) => "object",
        _ => null
    };
}