using Xunit;
using System.Windows.Controls;
using Factos;

namespace WPFTests;

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
        Assert.True(button.ActualWidth > 0); // ensure layout has occurred
    }

    [TestMethod]
    public void FailedTest()
    {
        Assert.Equal(8 + 2, 11);
    }
}
