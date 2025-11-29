using Xunit;
using Factos;
using Factos.Blazor;

namespace BlazorTests;

public class SomeTests
{
    [TestMethod]
    public async Task SuccessfulTest()
    {
        var app = (BlazorAppController)AppController.Current;

        var component = await app.NavigateToView<TestedComponent>();

        // experimental idea to interact with the UI
        // but for now the priority is to test other blazor components
        // not the DOM.
        // with this concept we could extend this library to build DOM tests.
        var buttonText = await component.GetButtonText();
        Assert.True("click me" == buttonText);
    }

    [TestMethod]
    [ExpectedToFail]
    public void FailedTest()
    {
        // when marked as ExpectedToFail, this test will be reported as passed only if it fails
        Assert.Equal(1 + 1, 3);
    }
}
