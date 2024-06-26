using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoProperty.Generator;

[Generator]
public class AutoPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create a simple filter to find classes that might implement interfaces
        IncrementalValuesProvider<string> pipeline = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => NodeIsEligibleForGeneration(node),
            transform: static (ctx, _) => TransformNode(ctx));

        context.RegisterSourceOutput(pipeline, GenerateOutput);
    }


    private static bool NodeIsEligibleForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };

    private static string TransformNode(GeneratorSyntaxContext generatorContext)
        => ((ClassDeclarationSyntax)generatorContext.Node).Identifier.ValueText;

    // type of second parameter is the type of the output of the transform function
    private static void GenerateOutput(SourceProductionContext context, string classIdentifier)
    {
        context.AddSource($"{classIdentifier}.g.cs", $"// Generated code for {classIdentifier}");
    }
}