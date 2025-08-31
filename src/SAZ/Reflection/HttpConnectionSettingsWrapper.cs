using System.Reflection;

namespace System.Net.Http;

public class HttpConnectionSettingsWrapper
{
    private static readonly Type Type;
    private static readonly FieldInfo MetricsField;

    static HttpConnectionSettingsWrapper()
    {
        Type = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpConnectionSettings", throwOnError: true)!;
        MetricsField = ReflectionHelper.GetInstanceField(Type, "_metrics");
    }

    public HttpConnectionSettingsWrapper()
    {
        Inner = Activator.CreateInstance(Type)!;
    }

    public object Inner { get; }

    public void SetMetrics(SocketsHttpHandlerMetricsWrapper socketsHttpHandlerMetricsWrapper)
    {
        MetricsField.SetValue(Inner, socketsHttpHandlerMetricsWrapper.Inner);
    }
}
