using Factos.Abstractions.Dto;
using System.Reflection;

namespace Factos.RemoteTesters;

public class SourceGeneratedTestExecutor
    : TestExecutor
{
    private static readonly List<TestInfo> _registeredTests = [];

    protected static T Register<T>(T instance, TestInfo[] tests)
    {
        _registeredTests.AddRange(tests);
        return instance;
    }

    internal override IAsyncEnumerable<TestNodeDto> Execute()
    {
        return GetResultsNodes();
    }

    private static async IAsyncEnumerable<TestNodeDto> GetResultsNodes(IEnumerable<TestInfo>? source = null)
    {
        // quick hack.. do not discover if source is provided
        // because it makes no sense for the intended usage of source
        if (source is not null)
            yield return new TestNodeDto()
            {
                Uid = TestNodeDto.DISCOVERED_NODES,
                DisplayName = "Discovered Tests",
                Properties = [],
                Children = [.. GetDiscoverNodes()]
            };

        foreach (var test in source ?? _registeredTests)
        {
            // === FILTERS ARE NOT IMPLEMENTED YET
            //if (runTestExecutionRequest.Filter is TestNodeUidListFilter filter)
            //    if (!filter.TestNodeUids.Any(testId => testId == $"{test.DeclaringType!.FullName}.{test.Name}"))
            //        continue;

            if (test.SkipReason is not null)
            {
                yield return new TestNodeDto()
                {
                    Uid = test.Uid,
                    DisplayName = test.DisplayName,
                    Properties = [new SkippedTestNodeStatePropertyDto { Explanation = test.SkipReason }],
                };

                continue;
            }

            var passed = false;
            PropertyDto[]? properties = null;

            try
            {
                await AppController.Current.InvokeOnUIThread(test.Invoker);
                properties = [new PassedTestNodeStatePropertyDto()];
                passed = true;
            }
            catch (TargetInvocationException ex)
            {
                properties = [new ErrorTestNodeStatePropertyDto { Explanation = ex.InnerException?.Message }];
            }
            catch (Exception ex)
            {
                var n = Environment.NewLine;
                properties = [new FailedTestNodeStatePropertyDto
                { 
                    Explanation = ex.Message + n + "Stack Trace:" + n + ex.StackTrace
                }];
            }

            if (test.ExpectFail)
            {
                properties = passed
                    ? [new FailedTestNodeStatePropertyDto { Explanation = "Test was expected to fail but passed." }]
                    : [new PassedTestNodeStatePropertyDto()];

                passed = !passed;
            }

            yield return new TestNodeDto
            {
                Uid = test.Uid,
                DisplayName = test.DisplayName,
                Properties = properties
            };

            if (!passed)
                yield break;
        }
    }

    private static IEnumerable<TestNodeDto> GetDiscoverNodes()
    {
        foreach (var test in _registeredTests)
        {
            var testNode = new TestNodeDto()
            {
                Uid = test.Uid,
                DisplayName = test.DisplayName,
                Properties = [new DiscoveredTestNodeStatePropertyDto(), test.Identifier]
            };

            yield return testNode;
        }
    }

    public static IAsyncEnumerable<TestNodeDto> GetResults(Func<TestInfo, bool> filter) =>
        GetResultsNodes(_registeredTests.Where(filter));
}
