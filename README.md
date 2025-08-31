# Knapcode.SAZ

Parse and enumerate the [Session Archive Zip (SAZ) file format](https://docs.telerik.com/fiddler-everywhere/knowledge-base/fiddler-archives) produced by Fiddler. Uses reflection over internal .NET HTTP types. Future .NET versions may require updates.

## Install

```
dotnet add package Knapcode.SAZ
```

## Example

```csharp
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
```

The output will look something like this:
```
Request: GET https://httpbin.org/get
Response: 200 OK
Body string length: 789
Started: 2025-08-30T11:33:40.0256586-07:00

Request: GET https://httpbin.org/stream/5
Response: 200 OK
Body string length: 3610
Started: 2025-08-30T11:34:28.6859373-07:00

Request: GET https://httpbin.org/brotli
Response: 200 OK
Body string length: 776
Started: 2025-08-30T11:34:46.7421843-07:00
```

## Disclaimers

This project is independent and has **no affiliation** with Telerik or Progress Software Corporation (makers of Fiddler).

Fiddler is a trademark of Progress Software Corporation. All product names, trademarks and registered trademarks are property of their respective owners.

WebSocket and gRPC sessions (`_w.txt`, `_g.txt`) are currently not supported.

The library uses *reflection on internal .NET runtime types* (e.g. `HttpConnection`) to parse HTTP messages efficiently without re‑implementing the HTTP protocol. These internal APIs are **unsupported** and can change between .NET releases. Forward compatibility is **not guaranteed**. If a future .NET version changes required internal members, the library should throw an exception (perhaps not very descriptive) instead of failing silently. Please pin a known working .NET runtime version in production, and run your test suite before upgrading.

Input validation is minimal: if you process untrusted SAZ files you may wish to impose additional limits (entry count, total uncompressed size) before calling `SessionArchiveZip.Create`.
