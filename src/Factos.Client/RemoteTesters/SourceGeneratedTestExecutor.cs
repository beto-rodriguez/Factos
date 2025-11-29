using Factos.Abstractions.Dto;
using System.Reflection;
using System.Text.Json;

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

    public override Task<string> Discover() =>
        Task.FromResult(JsonSerializer.Serialize(GetDiscoverNodes()));

    public override async Task<string> Run()
    {
        var nodes = await GetRunNodes();
        return JsonSerializer.Serialize(nodes);
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

    public async Task<List<TestNodeDto>> GetRunNodes()
    {
        var nodes = new List<TestNodeDto>();

        foreach (var test in _registeredTests)
        {
            // === FILTERS ARE NOT IMPLEMENTED YET
            //if (runTestExecutionRequest.Filter is TestNodeUidListFilter filter)
            //    if (!filter.TestNodeUids.Any(testId => testId == $"{test.DeclaringType!.FullName}.{test.Name}"))
            //        continue;

            if (test.SkipReason is not null)
            {
                nodes.Add(new TestNodeDto()
                {
                    Uid = test.Uid,
                    DisplayName = test.DisplayName,
                    Properties = [new SkippedTestNodeStatePropertyDto { Explanation = test.SkipReason }],
                });

                continue;
            }

            var passed = false;
            PropertyDto[]? properties = null;

            try
            {
                await AppController.Current.InvokeOnUIThread(test.Invoker());

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
            }

            nodes.Add(new TestNodeDto
            {
                Uid = test.Uid,
                DisplayName = test.DisplayName,
                Properties = properties
            });
        }

        return nodes;
    }
}