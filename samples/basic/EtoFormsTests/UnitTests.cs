using Eto.Forms;
using Factos;
using Xunit;

namespace EtoFormsTests;

public class SomeTests
{
    [TestMethod]
    public async Task SuccessfulTest()
    {
        var app = AppController.Current;

        var button = new Button { Text = "click me" };

        await app.NavigateToView(button);
        await app.WaitUntilLoaded(button);

        Assert.True("click me" ==  button.Text);
        Assert.True(button.Width > 0); // ensure layout has occurred
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
