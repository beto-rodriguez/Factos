namespace Factos.Protocols;

internal interface IProtocolHandler
{
    /// <summary>
    /// Executes the protocol handler with the given controller, this method will be callled
    /// multiple times until it returns true.
    /// </summary>
    /// <param name="controller">The app controller.</param>
    /// <returns>A value indicating whether the execution finished.</returns>
    Task<bool> Execute(AppController controller);
}
