namespace System.Net.Http;

public class HttpConnectionPoolWrapper
{
    private static readonly Type Type;
    private static readonly int MajorVersion;

    static HttpConnectionPoolWrapper()
    {
        Type = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpConnectionPool", throwOnError: true)!;
        MajorVersion = ReflectionHelper.GetAssemblyMajorVersion(Type);
    }

    public HttpConnectionPoolWrapper(HttpConnectionPoolManagerWrapper manager, HttpConnectionKindWrapper kind, string host, int port)
    {
        Inner = MajorVersion switch
        {
            >= 8 and <= 9 => Activator.CreateInstance(Type, [manager.Inner, kind.Inner, host, port, null, null])!,
            _ => Activator.CreateInstance(Type, [manager.Inner, kind.Inner, host, port, null, null, null])!,
        };
    }

    public HttpConnectionPoolWrapper(object inner)
    {
        ReflectionHelper.ThrowIfMismatchType(Type, inner);
        Inner = inner;
    }

    public object Inner { get; }
}
