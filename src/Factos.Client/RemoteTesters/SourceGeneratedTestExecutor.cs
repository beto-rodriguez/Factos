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

    private static async IAsyncEnumerable<TestNodeDto> GetResultsNodes(
        IEnumerable<TestInfo>? source = null)
    {
        var enumerator = new ResultsNodesEnumerator(source);

        try
        {
            while (await enumerator.MoveNextAsync())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
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

    private sealed class ResultsNodesEnumerator : IAsyncEnumerator<TestNodeDto>
    {
        private readonly IEnumerator<TestInfo> _tests;
        private readonly bool _includeDiscovered;
        private bool _yieldedDiscovered;
        private TestNodeDto? _current;

        public ResultsNodesEnumerator(IEnumerable<TestInfo>? source)
        {
            _tests = (source ?? _registeredTests).GetEnumerator();
            _includeDiscovered = source is not null;

            StreamHandler = new TestStreamHandler();
            StreamHandler.CancellationTokenSource.Token.Register(() =>
            {
                _current = new TestNodeDto
                {
                    Uid = _tests.Current.Uid,
                    DisplayName = _tests.Current.DisplayName,
                    Properties = [
                        new FailedTestNodeStatePropertyDto
                        {
                            Explanation = $"UI Thread error.\n"
                        }
                    ]
                };
                _tests.Dispose();
            });
        }

        public TestNodeDto Current => _current!;
        public TestStreamHandler StreamHandler { get; }

        public async ValueTask<bool> MoveNextAsync()
        {
            // First yield the "Discovered Tests" node if needed
            if (_includeDiscovered && !_yieldedDiscovered)
            {
                _yieldedDiscovered = true;
                _current = new TestNodeDto
                {
                    Uid = TestNodeDto.DISCOVERED_NODES,
                    DisplayName = "Discovered Tests",
                    Properties = [],
                    Children = [.. GetDiscoverNodes()]
                };
                return true;
            }

            if (!_tests.MoveNext())
                return false;

            var test = _tests.Current;

            if (test.SkipReason is not null)
            {
                _current = new TestNodeDto
                {
                    Uid = test.Uid,
                    DisplayName = test.DisplayName,
                    Properties = [new SkippedTestNodeStatePropertyDto { Explanation = test.SkipReason }]
                };
                return true;
            }

            var passed = false;
            PropertyDto[]? properties = null;

            try
            {
                await AppController.Current.InvokeOnUIThread(test.Invoker, StreamHandler);
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

            _current = new TestNodeDto
            {
                Uid = test.Uid,
                DisplayName = test.DisplayName,
                Properties = properties
            };

            // If not passed, stop iteration after yielding this one
            if (!passed)
            {
                // Mark enumerator as finished after this yield
                _tests.Dispose();
            }

            return true;
        }

        public ValueTask DisposeAsync()
        {
            _tests.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
