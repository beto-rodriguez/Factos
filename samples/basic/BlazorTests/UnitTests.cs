using Xunit;
using Factos;
using Factos.Blazor;

namespace BlazorTests;

public class SomeTests
{
    [UITestMethod]
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

    [UITestMethod]
    [ExpectedToFail]
    public void FailedTest()
    {
        // when marked as ExpectedToFail, this test will be reported as passed only if it fails
        Assert.Equal(1 + 1, 3);
    }

    [UITestMethod]
    [Skip("this test was skipped because...")]
    public void SkippedTest()
    {
        // this will not execute
    }

    [UITestMethod]
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
