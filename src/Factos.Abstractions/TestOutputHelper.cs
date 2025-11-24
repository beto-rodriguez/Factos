using System.Text;

namespace Factos.Abstractions;

internal sealed class TestOutputHelper : ITestOutputHelper
{
    public StringBuilder Output { get; set; } = new StringBuilder();
    public StringBuilder Error { get; set; } = new StringBuilder();

    public void WriteErrorLine(string message) => Error.AppendLine(message);

    public void WriteLine(string message) => Output.AppendLine(message);
}
