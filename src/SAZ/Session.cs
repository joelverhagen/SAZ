using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Knapcode.SAZ;

public class Session
{
    private readonly SemaphoreSlim? _readLock;

    public Session(string prefix, SemaphoreSlim? readLock)
    {
        _readLock = readLock;
        Prefix = prefix;
    }

    public string Prefix { get; }

    public ZipArchiveEntry? MetadataEntry { get; internal set; }
    public ZipArchiveEntry? RequestEntry { get; internal set; }
    public ZipArchiveEntry? ResponseEntry { get; internal set; }

    public async Task<SessionMetadata> ReadMetadataAsync(CancellationToken cancellationToken)
    {
        if (MetadataEntry is null)
        {
            throw new InvalidOperationException("No metadata entry associated with this session.");
        }

        using var stream = await WrapEntryStreamAsync(MetadataEntry);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, XmlResolver = null });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);

        if (document.Root is null)
        {
            throw new InvalidDataException("No root XML element found in the metadata stream.");
        }

        return SessionMetadata.FromXElement(document.Root!);
    }

    public async Task<HttpRequestMessage> ReadRequestAsync(bool decompress, CancellationToken cancellationToken)
    {
        if (RequestEntry is null)
        {
            throw new InvalidOperationException("No request entry associated with this session.");
        }

        var stream = await WrapEntryStreamAsync(RequestEntry);
        return await HttpSessionUtility.ParseHttpRequestMessageAsync(stream, decompress, cancellationToken);
    }

    public async Task<HttpResponseMessage> ReadResponseAsync(bool decompress, CancellationToken cancellationToken)
    {
        if (ResponseEntry is null)
        {
            throw new InvalidOperationException("No response entry associated with this session.");
        }

        var stream = await WrapEntryStreamAsync(ResponseEntry);
        return await HttpSessionUtility.ParseHttpResponseMessageAsync(stream, decompress, cancellationToken);
    }

    private async ValueTask<Stream> WrapEntryStreamAsync(ZipArchiveEntry entry)
    {
        if (_readLock is null)
        {
            return entry.Open();
        }

        await _readLock.WaitAsync();
        try
        {
            return new GatedReadStream(entry.Open(), _readLock);
        }
        finally
        {
            _readLock.Release();
        }
    }
}
