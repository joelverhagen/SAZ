using System.Xml.Linq;
using static Knapcode.SAZ.SessionMetadata;

namespace Knapcode.SAZ;

public class SessionTimers
{
    public DateTimeOffset? ClientConnected { get; set; }
    public DateTimeOffset? ClientBeginRequest { get; set; }
    public DateTimeOffset? GotRequestHeaders { get; set; }
    public DateTimeOffset? ClientDoneRequest { get; set; }
    public TimeSpan? GatewayTime { get; set; }
    public TimeSpan? DNSTime { get; set; }
    public TimeSpan? TCPConnectTime { get; set; }
    public TimeSpan? HTTPSHandshakeTime { get; set; }
    public DateTimeOffset? ServerConnected { get; set; }
    public DateTimeOffset? FiddlerBeginRequest { get; set; }
    public DateTimeOffset? ServerGotRequest { get; set; }
    public DateTimeOffset? ServerBeginResponse { get; set; }
    public DateTimeOffset? GotResponseHeaders { get; set; }
    public DateTimeOffset? ServerDoneResponse { get; set; }
    public DateTimeOffset? ClientBeginResponse { get; set; }
    public DateTimeOffset? ClientDoneResponse { get; set; }
    public Dictionary<string, string> UnrecognizedAttributes { get; } = new();

    public static SessionTimers FromXElement(XElement element)
    {
        var timers = new SessionTimers();

        foreach (var attribute in element.Attributes())
        {
            switch (attribute.Name.LocalName)
            {
                case "ClientConnected":
                    timers.ClientConnected = ParseDate(attribute);
                    break;
                case "ClientBeginRequest":
                    timers.ClientBeginRequest = ParseDate(attribute);
                    break;
                case "GotRequestHeaders":
                    timers.GotRequestHeaders = ParseDate(attribute);
                    break;
                case "ClientDoneRequest":
                    timers.ClientDoneRequest = ParseDate(attribute);
                    break;
                case "GatewayTime":
                    timers.GatewayTime = ParseTimeSpanFromMs(attribute);
                    break;
                case "DNSTime":
                    timers.DNSTime = ParseTimeSpanFromMs(attribute);
                    break;
                case "TCPConnectTime":
                    timers.TCPConnectTime = ParseTimeSpanFromMs(attribute);
                    break;
                case "HTTPSHandshakeTime":
                    timers.HTTPSHandshakeTime = ParseTimeSpanFromMs(attribute);
                    break;
                case "ServerConnected":
                    timers.ServerConnected = ParseDate(attribute);
                    break;
                case "FiddlerBeginRequest":
                    timers.FiddlerBeginRequest = ParseDate(attribute);
                    break;
                case "ServerGotRequest":
                    timers.ServerGotRequest = ParseDate(attribute);
                    break;
                case "ServerBeginResponse":
                    timers.ServerBeginResponse = ParseDate(attribute);
                    break;
                case "GotResponseHeaders":
                    timers.GotResponseHeaders = ParseDate(attribute);
                    break;
                case "ServerDoneResponse":
                    timers.ServerDoneResponse = ParseDate(attribute);
                    break;
                case "ClientBeginResponse":
                    timers.ClientBeginResponse = ParseDate(attribute);
                    break;
                case "ClientDoneResponse":
                    timers.ClientDoneResponse = ParseDate(attribute);
                    break;
                default:
                    timers.UnrecognizedAttributes[attribute.Name.LocalName] = attribute.Value;
                    break;
            }
        }

        return timers;
    }
}
