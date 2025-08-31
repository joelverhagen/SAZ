namespace Knapcode.SAZ;

public class SessionTests
{
    [Theory]
    [MemberData(nameof(SessionPrefixes))]
    public async Task CanReadMetadata(string path, string prefix)
    {
        var cancellationToken = CancellationToken.None;
        using var zipStream = File.OpenRead(path);
        using var saz = SessionArchiveZip.Create(zipStream);
        var session = saz.Sessions.Single(s => s.Prefix == prefix);

        var metadata = await session.ReadMetadataAsync(CancellationToken.None);

        await Verify(metadata).UseParameters(path,prefix).DontScrubDateTimes();
    }

    [Theory]
    [MemberData(nameof(SessionPrefixes))]
    public async Task CanReadRequest(string path, string prefix)
    {
        var cancellationToken = CancellationToken.None;
        using var zipStream = File.OpenRead(path);
        using var saz = SessionArchiveZip.Create(zipStream);
        var session = saz.Sessions.Single(s => s.Prefix == prefix);

        var request = await session.ReadRequestAsync(decompress: true, CancellationToken.None);

        await Verify(request).UseParameters(path, prefix).DontScrubDateTimes();
    }

    [Theory]
    [MemberData(nameof(SessionPrefixes))]
    public async Task CanReadResponse(string path, string prefix)
    {
        var cancellationToken = CancellationToken.None;
        using var zipStream = File.OpenRead(path);
        using var saz = SessionArchiveZip.Create(zipStream);
        var session = saz.Sessions.Single(s => s.Prefix == prefix);

        var response = await session.ReadResponseAsync(decompress: true, CancellationToken.None);

        await Verify(response).UseParameters(path, prefix).DontScrubDateTimes();
    }

    public static IEnumerable<object[]> SessionPrefixes
    {
        get
        {
            var pairs = new List<(string Path, string Prefix)>();
            foreach (var sazPath in Directory.EnumerateFiles("TestData", "*.saz"))
            {
                using var zipStream = File.OpenRead(sazPath);
                using var saz = SessionArchiveZip.Create(zipStream);
                pairs.AddRange(saz.Sessions.Select(s => (sazPath.Replace('\\', '/'), s.Prefix)));
            }

            return pairs.Select(p => new object[] { p.Path, p.Prefix } );
        }
    }
}
