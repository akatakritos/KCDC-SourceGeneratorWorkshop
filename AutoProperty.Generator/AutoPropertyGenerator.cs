using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoProperty.Generator;

internal record ClassToGenerate
{
    public string NamespaceName { get; set; }
    
    public string ClassName { get; set; }
    
    public EquatableArray<PropertyToGenerate> Properties { get; set; }
}


internal record PropertyToGenerate
{
    public string InterfaceName { get; set; }
    
    public string Visibility { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }

    public bool HasGet { get; set; }

    public bool HasSet { get; set; }

    public bool IsInit { get; set; }
}


[Generator]
public class AutoPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        
        var interfacePipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            "AutoProperty.Generator.AutoPropertyAttribute",
            predicate: (x, _) => true,
            transform: (x, _) =>
            {
                var interfaceNames = x.Attributes.Where(a => a.AttributeClass?.Name == "AutoPropertyAttribute")
                    .Select(a => (a.ConstructorArguments[0].Value as INamedTypeSymbol)?.MetadataName)
                    .ToArray();

                return new EquatableArray<string>(interfaceNames);
            });
        
        // Create a simple filter to find classes that might implement interfaces
        // change the return type to var for simplicity. 
        var pipeline = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: NodeIsEligibleForGeneration,
                transform: TransformNode)
            .Where(static x => x != null);
        
        var combinedPipeline = interfacePipeline
            .Combine(pipeline.Collect())
            .SelectMany(FlattenResult);

        context.RegisterSourceOutput(combinedPipeline, GenerateOutput);
        
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("AutoPropertyAttribute.g.cs", @"/// <auto-generated/>
using System;
namespace AutoProperty.Generator;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public class AutoPropertyAttribute(Type @interface) : Attribute 
{
}");
        });
    }
    
    private static ImmutableArray<ClassToGenerate> FlattenResult(
        (EquatableArray<string> Interfaces, ImmutableArray<ClassToGenerate> ClassesToGenerate) tuple,
        CancellationToken cancellationToken)
    {
        var classesToGenerate = tuple.ClassesToGenerate;
        var interfaces = tuple.Interfaces;

        List<ClassToGenerate> result = new();
        foreach (var classToGenerate in classesToGenerate)
        {
            var filteredProperties = classToGenerate.Properties.Where(p => interfaces.Contains(p.InterfaceName))
                .ToArray();

            if (filteredProperties.Length == 0)
            {
                continue;
            }

            result.Add(classToGenerate with
            {
                Properties = new EquatableArray<PropertyToGenerate>(filteredProperties)
            });
        }

        return result.ToImmutableArray();
    }


    private static bool NodeIsEligibleForGeneration(SyntaxNode node, CancellationToken cancellationToken = default)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };

    private static ClassToGenerate TransformNode(
        GeneratorSyntaxContext generatorContext,
        CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)generatorContext.Node;

        var symbol = generatorContext.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
        // Classes (and interfaces) are represented as an INamedTypeSymbol in the model
        // INamedTypeSymbol is conceptually similar to System.Type
        if (symbol is not INamedTypeSymbol classSymbol)
        {
            // this shouldn't happen, given we're filtering to ClassDeclarations. But having it puts a safe guard in place so the generator does fail/crash.
            return null;
        }
        
        // Add this before building the classProperties list
        if (classSymbol.AllInterfaces.Length == 0)
        {
            return null;
        }


        var classProperties = classSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .ToList();

// Get all the interfaces implemented by the class, and all properties from each interface.
        var interfaceProperties = classSymbol.AllInterfaces
            .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>())
            .ToList();

// Compare and filter out any properties that are already implemented by the class 
        var unimplementedProperties = interfaceProperties
            .Where(i => !classProperties.Exists(c => c.Name == i.Name))
            .Select(i => new PropertyToGenerate()
            {
                InterfaceName = i.ContainingType?.MetadataName,
                Visibility = "public",
                Type = i.Type.ToDisplayString(),
                Name = i.Name,
                HasGet = i.GetMethod != null,
                HasSet = i.SetMethod != null,
                IsInit = i.SetMethod is {IsInitOnly: true},
            })
            .ToArray();
        
        if (unimplementedProperties.Length == 0)
        {
            return null;
        }

        return new ClassToGenerate()
        {
            NamespaceName = classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name,
            Properties = new EquatableArray<PropertyToGenerate>(unimplementedProperties)
        };
    }

    // type of second parameter is the type of the output of the transform function
    private static void GenerateOutput(SourceProductionContext context, ClassToGenerate classToGenerate)
    {
        var formattedProperties = classToGenerate.Properties.Select(i =>
        {
            var getAccessor = i.HasGet ? "get;" : "";
            var setAccessor = i.HasSet ? "set;" : "";

            setAccessor = i.IsInit ? "init;" : setAccessor;

            return $"{i.Visibility} {i.Type} {i.Name} {{ {getAccessor} {setAccessor} }}";
        });

        var properties = string.Join("\n\n\t\t\t", formattedProperties);

        var sourceText = $$"""
                           /// <auto-generated>
                           /// UPDATED THIS TOOL
                           namespace {{classToGenerate.NamespaceName}}
                           {
                               partial class {{classToGenerate.ClassName}}
                               {
                                   {{properties}}
                               }
                           }
                           """;

        context.AddSource($"{classToGenerate.NamespaceName}.{classToGenerate.ClassName}.g.cs", sourceText);
    }
}