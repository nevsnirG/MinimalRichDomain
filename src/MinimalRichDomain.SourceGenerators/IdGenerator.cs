using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace MinimalRichDomain.SourceGenerators
{
    [Generator]
    public class IdGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var classesToGenerate = context.Compilation.SyntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>())
                .Where(classSyntax => classSyntax.AttributeLists.Any(IsGenerateIdAttribute))
                .ToList();

            foreach (var classSyntax in classesToGenerate)
            {
                var className = classSyntax.Identifier.Text;
                var idTypeName = $"{className}Id";

                var idCode = GenerateIdCode(idTypeName);

                context.AddSource($"{idTypeName}.g.cs", SourceText.From(idCode, Encoding.UTF8));
            }
        }

        private bool IsGenerateIdAttribute(AttributeListSyntax attributeList)
        {
            return attributeList.Attributes.Any(IsGenerateIdAttribute);
        }

        private static bool IsGenerateIdAttribute(AttributeSyntax attribute)
        {
            string attributeName = attribute.Name.ToString();
            return attributeName.Equals("GenerateId", StringComparison.InvariantCultureIgnoreCase) ||
                   attributeName.Equals("GenerateIdAttribute", StringComparison.InvariantCultureIgnoreCase);
        }

        private static string GenerateIdCode(string idTypeName)
        {
            return $@"
using System;

public readonly struct {idTypeName}
{{
    public Guid Value {{ get; }}

    private {idTypeName}(Guid value)
    {{
        Value = value;
    }}

    public static {idTypeName} New() => new(Guid.NewGuid());

    public static {idTypeName} FromValue(Guid value) => new(value);
}}
    ";
        }
    }
}