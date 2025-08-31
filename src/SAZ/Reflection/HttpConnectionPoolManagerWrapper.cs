namespace System.Net.Http;

public class HttpConnectionPoolManagerWrapper
{
    private static readonly Type Type;

    static HttpConnectionPoolManagerWrapper()
    {
        Type = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpConnectionPoolManager", throwOnError: true)!;
    }

    public HttpConnectionPoolManagerWrapper(HttpConnectionSettingsWrapper settings)
    {
        Inner = Activator.CreateInstance(Type, [settings.Inner])!;
    }

    public object Inner { get; }
}
