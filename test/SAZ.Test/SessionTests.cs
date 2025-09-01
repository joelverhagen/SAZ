namespace Knapcode.SAZ;

public class SessionTests
{
    [Fact]
    public async Task StressTest()
    {
        const int Iterations = 100;
        for (var i = 0; i < Iterations; i++)
        {
            for (var read = 0; read < 50; read++)
            {
                var cancellationToken = CancellationToken.None;
                var path = "TestData/images.saz";
                using var zipStream = File.OpenRead(path);
                using var saz = SessionArchiveZip.Create(zipStream);
                var buffer = new byte[read];

                foreach (var session in saz.Sessions)
                {
                    var metadata = await session.ReadMetadataAsync(cancellationToken);

                    using var request = await session.ReadRequestAsync(decompress: false, cancellationToken);
                    if (read > 0)
                    {
                        using var requestStream = await request.Content!.ReadAsStreamAsync(cancellationToken);
                        var bytesRead = await requestStream.ReadAsync(buffer, 0, read, cancellationToken);
                    }

                    using var response = await session.ReadResponseAsync(decompress: false, cancellationToken);
                    if (read > 0)
                    {
                        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                        var bytesRead = await responseStream.ReadAsync(buffer, 0, read, cancellationToken);
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(SessionPrefixes))]
    public async Task CanReadMetadata(string path, string prefix)
    {
        var cancellationToken = CancellationToken.None;
        using var zipStream = File.OpenRead(path);
        using var saz = SessionArchiveZip.Create(zipStream);
        var session = saz.Sessions.Single(s => s.Prefix == prefix);

        var metadata = await session.ReadMetadataAsync(CancellationToken.None);

        await VerifyAsync(metadata, path, prefix);
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

        await VerifyAsync(request, path, prefix);
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

        // clear the date because it gets formatted to local time by Verify.Http
        // https://github.com/VerifyTests/Verify.Http/blob/60264ac7819e03707736f0ddaa9f45d30734c8e4/src/Verify.Http/Extensions.cs#L22
        Assert.NotNull(response.Headers.Date);
        response.Headers.Date = null;

        await VerifyAsync(response, path, prefix);
    }

    private async Task VerifyAsync<T>(T obj, string path, string prefix)
    {
        Assert.NotNull(obj);

#if !NET8_0
        await Verify(obj).UseParameters(path, prefix);
#else
        await Task.Yield();
#endif
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

            return pairs.Select(p => new object[] { p.Path, p.Prefix });
        }
    }
}
