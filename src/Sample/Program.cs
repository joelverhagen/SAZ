using Knapcode.SAZ;

string path = "../../test/SAZ.Test/TestData/httpbin-test.saz";
CancellationToken token = CancellationToken.None;
using FileStream zipStream = File.OpenRead(path);
using SessionArchiveZip saz = SessionArchiveZip.Create(zipStream);

foreach (Session session in saz.Sessions.Take(3))
{
    // read the request as an HTTP request message
    using var request = await session.ReadRequestAsync(decompress: true, token);
    Console.WriteLine($"Request: {request.Method} {request.RequestUri}");

    // read the response as an HTTP response message
    using var response = await session.ReadResponseAsync(decompress: true, token);
    var responseBody = await response.Content.ReadAsStringAsync(token);
    Console.WriteLine($"Response: {(int)response.StatusCode} {response.ReasonPhrase}");
    Console.WriteLine($"Body string length: {responseBody.Length}");

    // read metadata about the request like timings
    var metadata = await session.ReadMetadataAsync(token);
    Console.WriteLine($"Started: {metadata.SessionTimers?.ClientBeginRequest:O}");
    Console.WriteLine();
}
