using System.Reflection;

namespace System.Net.Http;

public class ArrayBufferWrapper
{
    private static readonly Type Type;
    private static readonly FieldInfo BytesField;
    private static readonly FieldInfo ActiveStartField;
    private static readonly FieldInfo AvailableStartField;
    private static readonly MethodInfo DiscardMethod;

    static ArrayBufferWrapper()
    {
        Type = typeof(HttpClient).Assembly.GetType("System.Net.ArrayBuffer", throwOnError: true)!;
        BytesField = ReflectionHelper.GetInstanceField(Type, "_bytes");
        ActiveStartField = ReflectionHelper.GetInstanceField(Type, "_activeStart");
        AvailableStartField = ReflectionHelper.GetInstanceField(Type, "_availableStart");
        DiscardMethod = ReflectionHelper.GetInstanceMethod(Type, "Discard");
    }

    public object Inner;

    public ArrayBufferWrapper(object inner)
    {
        if (inner.GetType() != Type)
        {
            throw new ArgumentException($"Expected type '{Type.FullName}', but got '{inner.GetType().FullName}'.", nameof(inner));
        }

        Inner = inner;
    }

    public void Discard(int bytes)
    {
        DiscardMethod.Invoke(Inner, [bytes]);
    }

    public Span<byte> ActiveSpan
    {
        get
        {
            var bytes = (byte[])BytesField.GetValue(Inner)!;
            var activeStart = (int)ActiveStartField.GetValue(Inner)!;
            var availableStart = (int)AvailableStartField.GetValue(Inner)!;
            return new Span<byte>(bytes, activeStart, availableStart - activeStart);
        }
    }
}
