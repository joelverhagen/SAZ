using System.IO.Compression;

namespace Knapcode.SAZ;

public class SessionArchiveZip : IDisposable
{
    private readonly ZipArchive _zipArchive;

    public SessionArchiveZip(ZipArchive zipArchive, IReadOnlyList<Session> sessions)
    {
        _zipArchive = zipArchive;
        Sessions = sessions;
    }

    private const string SessionFilePrefix = "raw/";

    public static SessionArchiveZip Create(Stream zipStream)
    {
        var zipReader = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var filePrefixToSession = new Dictionary<string, Session>();
        var sessions = new List<Session>();

        foreach (var entry in zipReader.Entries)
        {
            if (!entry.FullName.StartsWith(SessionFilePrefix) || entry.FullName == SessionFilePrefix)
            {
                continue;
            }

            if (TryApply(filePrefixToSession, sessions, entry, "_c.txt", (s, e) => s.RequestEntry = entry))
            {
                continue;
            }

            if (TryApply(filePrefixToSession, sessions, entry, "_s.txt", (s, e) => s.ResponseEntry = entry))
            {
                continue;
            }

            if (TryApply(filePrefixToSession, sessions, entry, "_m.xml", (s, e) => s.MetadataEntry = entry))
            {
                continue;
            }

            throw new InvalidDataException($"Unsupported file found in archive {SessionFilePrefix} directory: {entry.FullName}");
        }

        return new SessionArchiveZip(zipReader, sessions);
    }

    private static bool TryApply(
        Dictionary<string, Session> filePrefixToSession,
        List<Session> sessions,
        ZipArchiveEntry entry,
        string suffix,
        Action<Session, ZipArchiveEntry> apply)
    {
        if (!entry.FullName.EndsWith(suffix))
        {
            return false;
        }

        var prefix = entry.FullName.Substring(SessionFilePrefix.Length, entry.FullName.Length - (suffix.Length + SessionFilePrefix.Length));
        if (!filePrefixToSession.TryGetValue(prefix, out var session))
        {
            session = new Session(prefix);
            sessions.Add(session);
            filePrefixToSession.Add(prefix, session);
        }

        apply(session, entry);
        return true;
    }

    public IReadOnlyList<Session> Sessions { get; }

    public void Dispose()
    {
        _zipArchive.Dispose();
    }
}
