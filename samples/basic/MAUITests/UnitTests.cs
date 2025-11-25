using Factos;
using Xunit;

namespace MAUITests;

public class SomeTests
{
    [TestMethod]
    public async Task SuccessfulTest()
    {
        var app = AppController.Current;
        var button = new Button { Text = "click me" };
        var page = new ContentPage { Content = button };

        await app.NavigateToView(page);
        await app.WaitUntilLoaded(button);

        Assert.True("click me" == button.Text);
        Assert.True(button.Width > 0); // ensure layout has occurred
    }

    [TestMethod]
    [ExpectedToFail]
    public void FailedTest()
    {
        // when marked as ExpectedToFail, this test will be reported as passed only if it fails
        Assert.Equal(1 + 1, 3);
    }
}
