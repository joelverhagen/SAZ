namespace System.Net.Http;

public class HttpConnectionKindWrapper
{
    private static readonly Type Type;

    static HttpConnectionKindWrapper()
    {
        Type = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpConnectionKind", throwOnError: true)!;
        Http = new HttpConnectionKindWrapper("Http");
    }

    public static HttpConnectionKindWrapper Http { get; } 

    private HttpConnectionKindWrapper(string name)
    {
        Inner = Enum.Parse(Type, name);
    }

    public object Inner { get; }
}
