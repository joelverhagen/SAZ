using System.Xml.Linq;
using static Knapcode.SAZ.SessionMetadata;

namespace Knapcode.SAZ;

public class TunnelInfo
{
    public long? BytesEgress { get; set; }
    public long? BytesIngress { get; set; }
    public Dictionary<string, string> UnrecognizedAttributes { get; } = new();

    public static TunnelInfo FromXElement(XElement element)
    {
        var output = new TunnelInfo();

        foreach (var attribute in element.Attributes())
        {
            switch (attribute.Name.LocalName)
            {
                case "BytesEgress":
                    output.BytesEgress = ParseLong(attribute);
                    break;
                case "BytesIngress":
                    output.BytesIngress = ParseLong(attribute);
                    break;
                default:
                    output.UnrecognizedAttributes[attribute.Name.LocalName] = attribute.Value;
                    break;
            }
        }

        return output;
    }
}
