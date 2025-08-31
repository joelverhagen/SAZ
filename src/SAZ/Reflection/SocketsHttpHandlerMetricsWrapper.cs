using System.Diagnostics.Metrics;

namespace System.Net.Http;

public class SocketsHttpHandlerMetricsWrapper
{
    private static readonly Type Type;

    static SocketsHttpHandlerMetricsWrapper()
    {
        var assembly = typeof(HttpClient).Assembly;
        Type = assembly.GetType("System.Net.Http.Metrics.SocketsHttpHandlerMetrics", throwOnError: true)!;
    }

    public SocketsHttpHandlerMetricsWrapper(Meter meter)
    {
        Inner = Activator.CreateInstance(Type, [meter])!;
    }

    public object Inner { get; }
}
