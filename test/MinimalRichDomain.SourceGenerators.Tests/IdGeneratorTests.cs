using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace MinimalRichDomain.SourceGenerators.Tests;

public class IdGeneratorTests
{
    [Fact]
    public void GivenEntity_CreatesIdInSameNamespace()
    {
        Compilation inputCompilation = CreateCompilation(@"
namespace Entities
{
    [GenerateId]
    public class Entity { }
}
");

        var generator = new IdGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        diagnostics.Should().BeEmpty();
        outputCompilation.SyntaxTrees.Should().HaveCount(2);

        var runResult = driver.GetRunResult();
        runResult.Diagnostics.Should().BeEmpty();
        runResult.GeneratedTrees.Should().HaveCount(1);
        runResult.Results[0].GeneratedSources.Should().ContainSingle().
            Which.SourceText.ToString().Should().Contain("namespace Entities").And.Contain("public readonly partial struct EntityId");
    }

    private static Compilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] {
                MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateIdAttribute).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}