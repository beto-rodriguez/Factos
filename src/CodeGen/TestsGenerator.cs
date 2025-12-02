using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CodeGen;

[Generator]
public class TestsGenerator : IIncrementalGenerator
{
    private readonly string TestMethodAttribute = "Factos.TestMethodAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var testMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TestMethodAttribute,
                predicate: static (syntaxNode, _) => syntaxNode is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetTestMethods(ctx.SemanticModel, ctx.TargetNode))
            .Where(static m => m is not null)
            .Collect()
            .Select((methods, _) => methods
                .GroupBy(prop => prop?.ContainingClass));

        // Generate source for each match
        context.RegisterSourceOutput(testMethods, (spc, groups) =>
        {
            var hasOne = false;

            foreach (var group in groups)
            {
                if (group is null || group.Key is null) continue;

                var sanitizedClassName = group.Key
                   .Replace("global::", "")
                   .Replace(".", "_")
                   .Replace("<", "_")
                   .Replace(">", "_");

                var sb = new StringBuilder();

                foreach (var possibleTest in group)
                {
                    if (possibleTest is not { } test) continue;

                    var mip =
            $@"new Factos.Abstractions.Dto.TestMethodIdentifierPropertyDto
            {{
                AssemblyFullName = ""{test.AssemblyFullName}"",
                Namespace = ""{test.Namespace}"",
                TypeName = ""{test.TypeName}"",
                MethodName = ""{test.Name}"",
                MethodArity = {test.MethodArity},
                ParameterTypeFullNames = {test.ParameterTypeFullNames},
                ReturnTypeFullName = ""{test.ReturnTypeFullName}""
            }}";

                    var t = test.IsStatic ? group.Key : sanitizedClassName;
                    var invoker = test.IsAsync
                        ? $@"async () => {{ await {t}.{test.Name}(); }}"
                        : $@"() => {{ {t}.{test.Name}(); return System.Threading.Tasks.Task.CompletedTask; }}";

                    sb.AppendLine($@"        new(
            ""{test.Uid}"",
            ""{test.DisplayName}"",
            {mip},
            {invoker},
            {(test.SkipReason is null ? "null" : $"\"{test.SkipReason}\"")},
            {test.ExpectFail.ToString().ToLower()}),");
                }

                var source =
@$"namespace Factos;

public partial class SGTests : Factos.RemoteTesters.SourceGeneratedTestExecutor
{{
    private static {group.Key} {sanitizedClassName} = Register<{group.Key}>(new {group.Key}(), [
{sb}
    ]);
}}
";
                hasOne = true;
                spc.AddSource($"SGTests.{sanitizedClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
            }

            if (!hasOne) return;

            var init =
@$"namespace Factos;

public static class Init
{{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {{
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Factos.SGTests).TypeHandle);
    }}
}}
";

            spc.AddSource($"Init.g.cs", SourceText.From(init, Encoding.UTF8));

        });
    }

    private static TestMethod? GetTestMethods(SemanticModel semanticModel, SyntaxNode declarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(declarationSyntax) is not IMethodSymbol symbol)
            return null; // something went wrong

        var n = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        // detect if is static

        var containingType = symbol.ContainingType;

        string? skipReason = null;
        var hasExpectFail = false;

        foreach (var attr in symbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();

            if (attrName == "Factos.SkipAttribute")
            {
                skipReason = attr.ConstructorArguments.Length == 0
                    ? "not specified"
                    : attr.ConstructorArguments[0].Value as string;
            }

            if (attrName == "Factos.ExpectedToFailAttribute")
            {
                hasExpectFail = true;
            }
        }

        return new TestMethod(
            symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            symbol.Name,
            symbol.IsStatic,
            symbol.IsAsync,
            containingType.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            containingType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            containingType.Name,
            symbol.Arity,
            $"[{symbol.Parameters.Aggregate(string.Empty, (a, b) => a + $@"""{b.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}"",")}]",
            symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            $"{containingType.Name}.{symbol.Name}",
            symbol.Name,
            skipReason,
            hasExpectFail);
    }
}
