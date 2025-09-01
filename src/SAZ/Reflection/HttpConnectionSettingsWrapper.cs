using System.Reflection;

namespace System.Net.Http;

public class HttpConnectionSettingsWrapper
{
    private static readonly Type Type;
    private static readonly FieldInfo MetricsField;
    private static readonly FieldInfo MaxResponseDrainSizeField;
    private static readonly FieldInfo MaxResponseDrainTimeField;

    static HttpConnectionSettingsWrapper()
    {
        Type = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpConnectionSettings", throwOnError: true)!;
        MetricsField = ReflectionHelper.GetInstanceField(Type, "_metrics");
        MaxResponseDrainSizeField = ReflectionHelper.GetInstanceField(Type, "_maxResponseDrainSize");
        MaxResponseDrainTimeField = ReflectionHelper.GetInstanceField(Type, "_maxResponseDrainTime");
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

    public void SetMaxResponseDrainSize(int size)
    {
        MaxResponseDrainSizeField.SetValue(Inner, size);
    }

    public void SetMaxResponseDrainTime(TimeSpan time)
    {
        MaxResponseDrainTimeField.SetValue(Inner, time);
    }
}
