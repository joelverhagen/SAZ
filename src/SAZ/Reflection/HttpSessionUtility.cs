using System.Diagnostics.Metrics;

namespace System.Net.Http;

public static class HttpSessionUtility
{
    static HttpSessionUtility()
    {
        var assembly = typeof(HttpClient).Assembly;
        MajorVersion = ReflectionHelper.GetAssemblyMajorVersion(typeof(HttpClient));
        if (MajorVersion < 6)
        {
            throw new NotSupportedException($"Unsupported .NET version: {MajorVersion}. Only .NET 6 and later are supported.");
        }

        var httpConnectionSettings = new HttpConnectionSettingsWrapper();

        if (MajorVersion >= 8)
        {
            var metrics = new SocketsHttpHandlerMetricsWrapper(new Meter("FakeMetric"));
            httpConnectionSettings.SetMetrics(metrics);
        }

        var httpConnectionPoolManager = new HttpConnectionPoolManagerWrapper(httpConnectionSettings);
        var httpConnectionKind = HttpConnectionKindWrapper.Http;

        HttpConnectionPool = new HttpConnectionPoolWrapper(httpConnectionPoolManager, httpConnectionKind, "localhost", 80);

        GZipDecompressedContentType = assembly.GetType("System.Net.Http.DecompressionHandler+GZipDecompressedContent", throwOnError: true)!;
        DeflateDecompressedContent = assembly.GetType("System.Net.Http.DecompressionHandler+DeflateDecompressedContent", throwOnError: true)!;
        BrotliDecompressedContent = assembly.GetType("System.Net.Http.DecompressionHandler+BrotliDecompressedContent", throwOnError: true)!;
    }

    public static HttpContent DecompressContent(HttpContent content)
    {
        var contentEncoding = content.Headers.ContentEncoding.LastOrDefault();
        if (string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
        {
            return (HttpContent)Activator.CreateInstance(GZipDecompressedContentType, [content])!;
        }
        else if (string.Equals(contentEncoding, "deflate", StringComparison.OrdinalIgnoreCase))
        {
            return (HttpContent)Activator.CreateInstance(DeflateDecompressedContent, [content])!;
        }
        else if (string.Equals(contentEncoding, "br", StringComparison.OrdinalIgnoreCase))
        {
            return (HttpContent)Activator.CreateInstance(BrotliDecompressedContent, [content])!;
        }
        else
        {
            return content;
        }
    }

    public static async Task<HttpResponseMessage> ParseHttpResponseMessageAsync(Stream stream, bool decompress, CancellationToken cancellationToken)
    {
        try
        {
            var httpConnection = CreateHttpConnection(stream);
            var response = await httpConnection.ParseResponseAsync(decompress, cancellationToken);
            return response;
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }

    public static async Task<HttpRequestMessage> ParseHttpRequestMessageAsync(Stream stream, bool decompress, CancellationToken cancellationToken)
    {
        try
        {
            var httpConnection = CreateHttpConnection(stream);
            var request = await httpConnection.ParseRequestAsync(decompress, cancellationToken);
            return request;
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }

    private static readonly int MajorVersion;
    private static readonly HttpConnectionPoolWrapper HttpConnectionPool;
    private static readonly Type GZipDecompressedContentType;
    private static readonly Type DeflateDecompressedContent;
    private static readonly Type BrotliDecompressedContent;

    private const int MaxResponseHeadersByteLength = 65536; // 64 KB, the default in HttpHandlerDefaults

    private static HttpConnectionWrapper CreateHttpConnection(Stream stream)
    {
        var httpConnection = new HttpConnectionWrapper(HttpConnectionPool, stream);
        httpConnection.AllowedReadLineBytes = MaxResponseHeadersByteLength;
        return httpConnection;
    }
}
