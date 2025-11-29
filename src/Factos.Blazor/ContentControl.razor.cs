using Microsoft.AspNetCore.Components;

namespace Factos.Blazor;

public partial class ContentControl
{
    private TaskCompletionSource? _tsc;
    public DynamicComponent DynamicComponent { get; private set; } = null!;
    public Type CurrentType { get; set; } = typeof(Welcome);
    public Dictionary<string, object>? CurrentParameters { get; set; }

    [Parameter]
    public ControllerSettings Settings { get; set; } = ControllerSettings.Default with { Protocol = ProtocolType.Http };

    internal TaskCompletionSource SetContent(Type type, Dictionary<string, object>? parameters = null)
    {
        _tsc = new TaskCompletionSource();
        CurrentType = type;
        CurrentParameters = parameters;
        StateHasChanged();
        return _tsc;
    }

    override protected void OnInitialized()
    {
        base.OnInitialized();
        BlazorAppController.Content = this;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        _tsc?.SetResult();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender) return;

        var controller = new BlazorAppController(Settings);
        await AppController.InitializeController(controller);
    }
}
