using System.Xml.Linq;

namespace Knapcode.SAZ;

public class SessionFlags
{
    public Dictionary<string, string> Flags { get; set; } = new();
    public List<XElement> UnrecognizedChildren { get; } = new();

    public static SessionFlags FromXElement(XElement element)
    {
        var output = new SessionFlags();

        foreach (var child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "SessionFlag":
                    var flag = SessionFlagFromXElement(child);
                    output.Flags.Add(flag.Name, flag.Value);
                    break;
                default:
                    output.UnrecognizedChildren.Add(child);
                    break;
            }
        }

        return output;
    }

    private static (string Name, string Value) SessionFlagFromXElement(XElement element)
    {
        string? name = null;
        string? value = null;

        foreach (var attribute in element.Attributes())
        {
            switch (attribute.Name.LocalName)
            {
                case "N":
                    name = attribute.Value;
                    break;
                case "V":
                    value = attribute.Value;
                    break;
            }
        }
        
        if (name is null || value is null)
        {
            throw new InvalidDataException($"{element.Name.LocalName} element is missing required attributes.");
        }

        return (name, value);
    }
}
