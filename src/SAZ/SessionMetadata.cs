using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Knapcode.SAZ;

public class SessionMetadata
{
    public string? SID { get; set; }
    public uint? BitFlags { get; set; }
    public Dictionary<string, string> UnrecognizedAttributes { get; } = new();

    public SessionTimers? SessionTimers { get; set; }
    public PipeInfo? PipeInfo { get; set; }
    public TunnelInfo? TunnelInfo { get; set; }
    public SessionFlags? SessionFlags { get; set; }

    public static async Task<SessionMetadata> FromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, XmlResolver = null });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);

        if (document.Root is null)
        {
            throw new InvalidDataException("No root XML element found in the metadata stream.");
        }

        return FromXElement(document.Root!);
    }

    public static SessionMetadata FromXElement(XElement element)
    {
        var output = new SessionMetadata();

        foreach (var attribute in element.Attributes())
        {
            switch (attribute.Name.LocalName)
            {
                case "SID":
                    output.SID = attribute.Value;
                    break;
                case "BitFlags":
                    output.BitFlags = uint.Parse(attribute.Value, NumberStyles.HexNumber);
                    break;
                default:
                    output.UnrecognizedAttributes[attribute.Name.LocalName] = attribute.Value;
                    break;
            }
        }

        foreach (var child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "SessionTimers":
                    output.SessionTimers = SessionTimers.FromXElement(child);
                    break;
                case "PipeInfo":
                    output.PipeInfo = PipeInfo.FromXElement(child);
                    break;
                case "TunnelInfo":
                    output.TunnelInfo = TunnelInfo.FromXElement(child);
                    break;
                case "SessionFlags":
                    output.SessionFlags = SessionFlags.FromXElement(child);
                    break;
            }
        }

        return output;
    }

    internal static DateTimeOffset ParseDate(XAttribute attribute)
    {
        return DateTimeOffset.Parse(attribute.Value);
    }

    internal static TimeSpan ParseTimeSpanFromMs(XAttribute attribute)
    {
        return TimeSpan.FromMilliseconds(double.Parse(attribute.Value));
    }

    internal static bool ParseBoolean(XAttribute attribute)
    {
        return bool.Parse(attribute.Value);
    }

    internal static long ParseLong(XAttribute attribute)
    {
        return long.Parse(attribute.Value);
    }
}
