using Factos.Abstractions;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace Factos.Blazor;

public partial class ContentControl
{
    private TaskCompletionSource? _tsc;
    public DynamicComponent DynamicComponent { get; private set; } = null!;
    public Type CurrentType { get; set; } = typeof(Welcome);
    public Dictionary<string, object>? CurrentParameters { get; set; }

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

        var assembly = Assembly.GetExecutingAssembly() ?? throw new Exception("ASssembly not found");
        var controller = new BlazorAppController(Constants.DEFAULT_TCP_PORT);
        await AppController.InitializeController(controller, false);
    }
}
