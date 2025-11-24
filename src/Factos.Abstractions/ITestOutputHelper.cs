namespace Factos.Abstractions;

public interface ITestOutputHelper
{
    void WriteLine(string message);
    void WriteErrorLine(string message);
}
