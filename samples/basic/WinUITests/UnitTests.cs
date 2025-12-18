using Xunit;
using Factos;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace WinUITests;

public class SomeTests
{
    [AppTestMethod]
    public async Task SuccessfulTest()
    {
        var app = AppController.Current;

        var button = new Button { Content = "click me" };

        await app.NavigateToView(button);
        await app.WaitUntilLoaded(button);

        Assert.True("click me" ==  button.Content.ToString());
        Assert.True(button.ActualWidth > 0); // ensure layout has occurred
    }

    [AppTestMethod]
    [ExpectedToFail]
    public void FailedTest()
    {
        // when marked as ExpectedToFail, this test will be reported as passed only if it fails
        Assert.Equal(1 + 1, 3);
    }

    [AppTestMethod]
    [Skip("this test was skipped because...")]
    public void SkippedTest()
    {
        // this will not execute
    }

    [AppTestMethod]
    public async Task EnsureTestsResultsAreCorrect()
    {
        // this method is what actually runs test internally in the testing framework
        // lets just ensure that the results are as expected
        var testsStream = Factos.RemoteTesters.SourceGeneratedTestExecutor
            .GetResults(x => x.DisplayName != nameof(EnsureTestsResultsAreCorrect));

        int passed = 0, failed = 0, skipped = 0;

        await foreach (var test in testsStream)
        {
            foreach (var property in test.Properties)
            {
                if (property is Factos.Abstractions.Dto.SkippedTestNodeStatePropertyDto)
                    skipped++;
                else if (property is Factos.Abstractions.Dto.FailedTestNodeStatePropertyDto)
                    failed++;
                else if (property is Factos.Abstractions.Dto.PassedTestNodeStatePropertyDto)
                    passed++;
            }
        }

        Assert.Equal(2, passed);
        Assert.Equal(1, skipped);
    }
}
