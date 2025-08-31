using System.Reflection;

namespace System.Net.Http;

public class ReflectionHelper
{
    public static void ThrowIfMismatchType(Type type, object inner)
    {
        if (inner.GetType() != type)
        {
            throw new ArgumentException($"Expected type '{type.FullName}', but got '{inner.GetType().FullName}'.", nameof(inner));
        }
    }

    public static int GetAssemblyMajorVersion(Type type) => type.Assembly.GetName().Version!.Major;

    public static PropertyInfo GetInstanceProperty(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is null)
        {
            throw new InvalidOperationException($"Unable to find instance property '{propertyName}' on type '{type.FullName}'.");
        }

        return property;
    }

    public static FieldInfo GetInstanceField(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null)
        {
            throw new InvalidOperationException($"Unable to find instance field '{fieldName}' on type '{type.FullName}'.");
        }

        return field;
    }

    public static MethodInfo GetStaticMethod(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (method is null)
        {
            throw new InvalidOperationException($"Unable to find static method '{methodName}' on type '{type.FullName}'.");
        }

        return method;
    }

    public static MethodInfo GetInstanceMethod(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null)
        {
            throw new InvalidOperationException($"Unable to find instance method '{methodName}' on type '{type.FullName}'.");
        }

        return method;
    }
}
