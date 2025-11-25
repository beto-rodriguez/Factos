using Factos.Abstractions.Dto;
using System.Reflection;
using System.Text.Json;

namespace Factos.RemoteTesters;

public class ReflectionTestExecutor(Assembly assembly) 
    : TestExecutor
{
    public override Task<string> Discover(string command) =>
        Task.FromResult(JsonSerializer.Serialize(GetDiscoverNodes()));

    public override async Task<string> Run(string command)
    {
        var nodes = await GetRunNodes();
        return JsonSerializer.Serialize(nodes);
    }

    private IEnumerable<TestNodeDto> GetDiscoverNodes()
    {
        foreach (var test in GetTestsMethodFromAssemblies())
        {
            var testMethodIdentifierProperty =
                new TestMethodIdentifierPropertyDto
                {
                    AssemblyFullName = test.DeclaringType!.Assembly!.FullName!,
                    Namespace = test.DeclaringType!.Namespace!,
                    TypeName = test.DeclaringType.Name!,
                    MethodName = test.Name,
                    MethodArity = test.GetGenericArguments().Length,
                    ParameterTypeFullNames = test.GetParameters().Select(x => x.ParameterType.FullName).ToArray()!,
                    ReturnTypeFullName = test.ReturnType.FullName!
                };

            var testNode = new TestNodeDto()
            {
                Uid = $"{test.DeclaringType!.FullName}.{test.Name}",
                DisplayName = test.Name,
                Properties = [new DiscoveredTestNodeStatePropertyDto(), testMethodIdentifierProperty]
            };

            yield return testNode;
        }
    }

    public async Task<List<TestNodeDto>> GetRunNodes()
    {
        var nodes = new List<TestNodeDto>();

        foreach (var test in GetTestsMethodFromAssemblies())
        {
            // === FILTERS ARE NOT IMPLEMENTED YET
            //if (runTestExecutionRequest.Filter is TestNodeUidListFilter filter)
            //    if (!filter.TestNodeUids.Any(testId => testId == $"{test.DeclaringType!.FullName}.{test.Name}"))
            //        continue;

            var skipAttribute = test.GetCustomAttribute<SkipAttribute>();

            if (skipAttribute is not null)
            {
                nodes.Add(new TestNodeDto()
                {
                    Uid = $"{test.DeclaringType!.FullName}.{test.Name}",
                    DisplayName = test.Name,
                    Properties = [new SkippedTestNodeStatePropertyDto { Explanation = skipAttribute.Reason }],
                });

                continue;
            }

            var passed = false;
            object? instance = Activator.CreateInstance(test.DeclaringType!);
            PropertyDto[]? properties = null;

            try
            {
                var app = AppController.Current;

                async Task InvokeMethodInfo()
                {
                    var result = test.Invoke(instance, null);

                    if (result is Task task)
                        await task;
                }

                await app.InvokeOnUIThread(InvokeMethodInfo());

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

            var expectedToFail = test.GetCustomAttribute<ExpectedToFailAttribute>() is not null;
            if (expectedToFail)
            {
                properties = passed
                    ? [new FailedTestNodeStatePropertyDto { Explanation = "Test was expected to fail but passed." }]
                    : [new PassedTestNodeStatePropertyDto()];
            }

            nodes.Add(new TestNodeDto
            {
                Uid = $"{test.DeclaringType!.FullName}.{test.Name}",
                DisplayName = test.Name,
                Properties = properties
            });
        }

        return nodes;
    }

    private MethodInfo[] GetTestsMethodFromAssemblies()
        => [.. assembly.GetTypes()
            .SelectMany(x => x.GetMethods())
            .Where(x => x.GetCustomAttributes<TestMethodAttribute>().Any())];
}