using System.Xml.Linq;
using static Knapcode.SAZ.SessionMetadata;

namespace Knapcode.SAZ;

public class PipeInfo
{
    public bool? CltReuse { get; private set; }
    public bool? Reused { get; private set; }

    public Dictionary<string, string> UnrecognizedAttributes { get; } = new();

    public static PipeInfo FromXElement(XElement element)
    {
        var output = new PipeInfo();

        foreach (var attribute in element.Attributes())
        {
            switch (attribute.Name.LocalName)
            {
                case "CltReuse":
                    output.CltReuse = ParseBoolean(attribute);
                    break;
                case "Reused":
                    output.Reused = ParseBoolean(attribute);
                    break;
                default:
                    output.UnrecognizedAttributes[attribute.Name.LocalName] = attribute.Value;
                    break;
            }
        }

        return output;
    }
}
