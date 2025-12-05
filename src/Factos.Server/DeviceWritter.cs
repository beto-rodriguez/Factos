using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;
using System.Text;

namespace Factos.Server;

public class DeviceWritter(IOutputDeviceDataProducer dataProducer, IOutputDevice outputDevice)
{
    const int SeparatorLength = 36;
    const char Separator = '-';

    public async Task Write(string message, ConsoleColor? color, CancellationToken? cancellationToken)
    {
        cancellationToken ??= CancellationToken.None;

        var data = color is null
            ? new TextOutputDeviceData(message)
            : new FormattedTextOutputDeviceData(message)
            {
                ForegroundColor = new SystemConsoleColor()
                {
                    ConsoleColor = color.Value
                }
            };

        await outputDevice.DisplayAsync(dataProducer, data, cancellationToken.Value);
    }

    public async Task Dimmed(string message, CancellationToken? cancellationToken) =>
        await Write(message, ConsoleColor.DarkGray, cancellationToken);

    public async Task Normal(string message, CancellationToken? cancellationToken) =>
        await Write(message, null, cancellationToken);

    public async Task Blue(string message, CancellationToken? cancellationToken) =>
        await Write(message, ConsoleColor.Blue, cancellationToken);

    public async Task Green(string message, CancellationToken? cancellationToken) =>
        await Write(message, ConsoleColor.Green, cancellationToken);

    public async Task Red(string message, CancellationToken? cancellationToken) =>
        await Write(message, ConsoleColor.Red, cancellationToken);

    public async Task Title(string message, CancellationToken? cancellationToken, bool condensed = false)
    {
        var nl = condensed ? "" : "\n";

        await Write(
            $"{nl}--- {(message + " ").PadRight(SeparatorLength, Separator)}{nl}",
            ConsoleColor.DarkGray, cancellationToken);
    }

    public async Task Banner(string message, CancellationToken? cancellationToken)
    {
        // lets create a banner like:
        // we receive a single line message
        // then we create multiple lines, with a max length of 36 characters

        // +----------------------------+
        // |  the message must support  |
        // |  multiple lines            |
        // +----------------------------+

        var sb = new StringBuilder();
        var lines = new List<string>();
        var words = message.Split(' ');
        var currentLine = new StringBuilder();
        
        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > 36)
            {
                lines.Add(currentLine.ToString().TrimEnd());
                currentLine.Clear();
            }
            currentLine.Append(word + " ");
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString().TrimEnd());
        
        var border = "+" + new string('-', 36) + "+";
        sb.AppendLine(border);
        
        foreach (var line in lines)
            sb.AppendLine($"| {line.PadRight(34)} |");

        sb.AppendLine(border);

        await Write(sb.ToString(), ConsoleColor.Yellow, cancellationToken);
    }
}