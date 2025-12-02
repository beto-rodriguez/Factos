using Avalonia.Controls;
using Factos;
using System.Threading.Tasks;
using Xunit;

namespace AvaloniaTests;

public class SomeTests
{
    [TestMethod]
    public async Task SuccessfulTest()
    {
        var app = AppController.Current;

        var button = new Button { Content = "click me" };

        await app.NavigateToView(button);
        await app.WaitUntilLoaded(button);

        Assert.True("click me" ==  button.Content.ToString());
        Assert.True(button.Bounds.Size.Width > 0); // ensure layout has occurred
    }

    [TestMethod]
    [ExpectedToFail]
    public void FailedTest()
    {
        // when marked as ExpectedToFail, this test will be reported as passed only if it fails
        Assert.Equal(1 + 1, 3);
    }

    [TestMethod]
    [Skip("this test was skipped because...")]
    public void SkippedTest()
    {
        // this will not execute
    }
}
