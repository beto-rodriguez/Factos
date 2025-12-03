using Eto.Forms;
using Factos;
using Xunit;

namespace EtoFormsTests;

public class SomeTests
{
    [AppTestMethod]
    public async Task SuccessfulTest()
    {
        var app = AppController.Current;

        var button = new Button { Text = "click me" };

        await app.NavigateToView(button);
        await app.WaitUntilLoaded(button);

        Assert.True("click me" ==  button.Text);
        Assert.True(button.Width > 0); // ensure layout has occurred
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
        var resultNodes = await Factos.RemoteTesters.SourceGeneratedTestExecutor
            .GetResults(x => x.DisplayName != nameof(EnsureTestsResultsAreCorrect));

        int passed = 0, failed = 0, skipped = 0;

        foreach (var test in resultNodes)
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
