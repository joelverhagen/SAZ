using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace System.Net.Http;

public class HttpConnectionWrapper
{
    private static readonly Type Type;
    private static readonly int MajorVersion;
    private static readonly FieldInfo AllowedReadLineBytesField;
    private static readonly MethodInfo ThrowExceededAllowedReadLineBytesMethod;
    private static readonly MethodInfo ParseStatusLineMethod;
    private static readonly MethodInfo? ParseHeadersMethod;
    private static readonly MethodInfo? FillForHeadersAsyncMethod;
    private static readonly FieldInfo? ReadBufferField;
    private static readonly Type ChunkedEncodingReadStreamType;
    private static readonly Type ContentLengthReadStreamType;
    private static readonly Type ConnectionCloseReadStreamType;

    static HttpConnectionWrapper()
    {
        Type = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpConnection", throwOnError: true)!;
        MajorVersion = ReflectionHelper.GetAssemblyMajorVersion(Type);
        AllowedReadLineBytesField = ReflectionHelper.GetInstanceField(Type, "_allowedReadLineBytes");
        ThrowExceededAllowedReadLineBytesMethod = ReflectionHelper.GetInstanceMethod(Type, "ThrowExceededAllowedReadLineBytes");

        ParseStatusLineMethod = ReflectionHelper.GetInstanceMethod(Type, "ParseStatusLine");
        ParseHeadersMethod = ReflectionHelper.GetInstanceMethod(Type, "ParseHeaders");
        FillForHeadersAsyncMethod = ReflectionHelper.GetInstanceMethod(Type, "FillForHeadersAsync");
        ReadBufferField = ReflectionHelper.GetInstanceField(Type, "_readBuffer");

        ChunkedEncodingReadStreamType = Type.Assembly.GetType("System.Net.Http.HttpConnection+ChunkedEncodingReadStream", throwOnError: true)!;
        ContentLengthReadStreamType = Type.Assembly.GetType("System.Net.Http.HttpConnection+ContentLengthReadStream", throwOnError: true)!;
        ConnectionCloseReadStreamType = Type.Assembly.GetType("System.Net.Http.HttpConnection+ConnectionCloseReadStream", throwOnError: true)!;
    }

    public object Inner { get; }

    public HttpConnectionWrapper(HttpConnectionPoolWrapper pool, Stream stream)
    {
        Inner = MajorVersion switch
        {
            8 => Activator.CreateInstance(Type, [pool.Inner, stream, null, null])!,
            _ => Activator.CreateInstance(Type, [pool.Inner, stream, null, null, null])!,
        };
    }

    public HttpConnectionWrapper(object inner)
    {
        ReflectionHelper.ThrowIfMismatchType(Type, inner);
        Inner = inner;
    }

    public int AllowedReadLineBytes
    {
        get => (int)AllowedReadLineBytesField.GetValue(Inner)!;
        set => AllowedReadLineBytesField.SetValue(Inner, value);
    }

    public ArrayBufferWrapper ReadBuffer
    {
        get => new ArrayBufferWrapper(ReadBufferField!.GetValue(Inner)!);
        set => ReadBufferField!.SetValue(Inner, value.Inner);
    }

    public async Task<HttpResponseMessage> ParseResponseAsync(bool decompress, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage();

        cancellationToken.ThrowIfCancellationRequested();

        while (!ParseStatusLine(response))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await FillForHeadersAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (response.StatusCode < HttpStatusCode.OK)
        {
            throw new NotImplementedException("Informational responses are not supported.");
        }

        while (!ParseHeaders(response))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await FillForHeadersAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        HttpContent content;
        if (response.Headers.TransferEncodingChunked == true)
        {
            content = new StreamContent(CreateChunkedEncodingReadStream(Inner, response));
        }
        else if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > 0)
        {
            content = new StreamContent(CreateContentLengthReadStream(Inner, (ulong)response.Content.Headers.ContentLength.Value));
        }
        else
        {
            content = new StreamContent(CreateConnectionCloseReadStream(Inner));
        }

        foreach (var header in response.Content.Headers.NonValidated)
        {
            if (!content.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                throw new InvalidOperationException($"Header '{header.Key}' could not be added to the content.");
            }
        }

        if (decompress)
        {
            response.Content = HttpSessionUtility.DecompressContent(content);
        }

        return response;
    }

    private bool ParseHeaders(HttpResponseMessage response)
    {
        return (bool)ParseHeadersMethod!.Invoke(Inner, [response, false])!;
    }

    private bool ParseStatusLine(HttpResponseMessage response)
    {
        return (bool)ParseStatusLineMethod.Invoke(Inner, [response])!;
    }

    private ValueTask FillForHeadersAsync()
    {
        return (ValueTask)FillForHeadersAsyncMethod!.Invoke(Inner, [true])!;
    }

    public async Task<HttpRequestMessage> ParseRequestAsync(bool decompress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new HttpRequestMessage();

        while (!ParseRequestLine(request))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await FillForHeadersAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        var response = new HttpResponseMessage();
        while (!ParseHeaders(response))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await FillForHeadersAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (response.Headers.TransferEncodingChunked == true)
        {
            request.Content = new StreamContent(CreateChunkedEncodingReadStream(Inner, response: null));
        }
        else if (response.Content.Headers.ContentLength.HasValue == true && response.Content.Headers.ContentLength.Value > 0)
        {
            request.Content = new StreamContent(CreateContentLengthReadStream(Inner, (ulong)response.Content.Headers.ContentLength.Value));
        }
        else
        {
            request.Content = new StreamContent(CreateConnectionCloseReadStream(Inner));
        }

        foreach (var header in response.Headers.NonValidated.Concat(response.Content.Headers.NonValidated))
        {
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                if (!request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    throw new InvalidOperationException($"Header '{header.Key}' could not be added to the request.");
                }
            }
        }

        if (decompress)
        {
            request.Content = HttpSessionUtility.DecompressContent(request.Content);
        }

        return request;
    }

    private bool ParseRequestLine(HttpRequestMessage request)
    {
        var readBuffer = ReadBuffer;
        var buffer = readBuffer.ActiveSpan;

        int lineFeedIndex = buffer.IndexOf((byte)'\n');
        if (lineFeedIndex >= 0)
        {
            int bytesConsumed = lineFeedIndex + 1;
            readBuffer.Discard(bytesConsumed);
            ReadBuffer = readBuffer; // mutable struct, so write back
            AllowedReadLineBytes -= bytesConsumed;

            int carriageReturnIndex = lineFeedIndex - 1;
            int length = (uint)carriageReturnIndex < (uint)buffer.Length && buffer[carriageReturnIndex] == '\r'
                ? carriageReturnIndex
                : lineFeedIndex;

            ParseRequestLineCore(buffer.Slice(0, length), request);
            return true;
        }
        else
        {
            if (AllowedReadLineBytes <= buffer.Length)
            {
                ThrowExceededAllowedReadLineBytesMethod.Invoke(Inner, null);
            }

            return false;
        }
    }

    private static void ParseRequestLineCore(Span<byte> span, HttpRequestMessage request)
    {
        var requestLine = Encoding.UTF8.GetString(span);
        var endOfVerb = span.IndexOf((byte)' ');
        if (endOfVerb < 1)
        {
            throw new InvalidDataException($"No HTTP method (verb) was found on the first line. First line: {requestLine}");
        }

        var method = Encoding.UTF8.GetString(span.Slice(0, endOfVerb));
        request.Method = new HttpMethod(method);

        var endOfRequestUri = span.LastIndexOf((byte)' ');
        if (endOfRequestUri <= endOfVerb)
        {
            throw new InvalidDataException($"No request URI was found on the first line. First line: {requestLine}");
        }

        var requestUri = Encoding.UTF8.GetString(span.Slice(endOfVerb + 1, endOfRequestUri - endOfVerb - 1));
        request.RequestUri = new Uri(requestUri, UriKind.RelativeOrAbsolute);

        var version = Encoding.UTF8.GetString(span.Slice(endOfRequestUri + 1));
        const string versionPrefix = "HTTP/";
        if (!version.StartsWith(version, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Invalid HTTP version was found on the first line. First line: {requestLine}");
        }

        if (!Version.TryParse(version.AsSpan(versionPrefix.Length), out var parsedVersion))
        {
            throw new InvalidDataException($"Unable to parse HTTP version was found on the first line. First line: {requestLine}");
        }

        request.Version = parsedVersion;
    }

    private static Stream CreateChunkedEncodingReadStream(object httpConnection, HttpResponseMessage? response)
    {
        return (Stream)Activator.CreateInstance(ChunkedEncodingReadStreamType, [httpConnection, response])!;
    }

    private static Stream CreateContentLengthReadStream(object httpConnection, ulong contentLength)
    {
        return (Stream)Activator.CreateInstance(ContentLengthReadStreamType, [httpConnection, contentLength])!;
    }

    private static Stream CreateConnectionCloseReadStream(object httpConnection)
    {
        return (Stream)Activator.CreateInstance(ConnectionCloseReadStreamType, [httpConnection])!;
    }
}
