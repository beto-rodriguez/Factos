using Microsoft.Testing.Platform.Extensions;

namespace Factos.Server;

internal abstract class BaseExtension : IExtension
{
    public string Uid => Id;

    public string Version => "1.0.0";

    public string DisplayName => Id;

    public virtual string Description => 
        $"An MTP extension for the {nameof(Factos)} testing framework.";

    protected abstract string Id { get; }

    public Task<bool> IsEnabledAsync() => 
        Task.FromResult(true);
}