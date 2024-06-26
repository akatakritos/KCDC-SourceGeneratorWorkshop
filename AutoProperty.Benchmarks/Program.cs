// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var summary = BenchmarkRunner.Run<KindTest>();
Console.WriteLine(summary);

public class KindTest
{
    
    private const string SourceCodeText = """
                                          namespace AutoProperty.Sample;

                                          public interface IAuditMetadata
                                          {
                                              DateTimeOffset LastUpdated { get; set; }
                                          }

                                          public partial class Book : IAuditMetadata
                                          {
                                              public required string Title { get; set; }
                                          
                                              public required string Author { get; set; }
                                          
                                              public DateTimeOffset LastUpdated { get; set; }
                                          }
                                          """;

    private CSharpCompilation _compilation;
    private SyntaxNode? _node;
    public KindTest()
    {
        _compilation = CSharpCompilation.Create(nameof(KindTest),
            // Add our source code to the compilation
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(SourceCodeText)
            ],
            // Add a reference to 'System.Private.CoreLib' for System types
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);
        _node = _compilation.SyntaxTrees[0].GetRoot().DescendantNodes().First();
        
    }

    [Benchmark]
    public bool KindCheck()
    {
        foreach (var node in _compilation.SyntaxTrees[0].GetRoot().DescendantNodes())
        {
            if (node.IsKind(SyntaxKind.ClassDeclaration) &&
                node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 })
            {
                return true;
            }
        }

        return false;
    }

    [Benchmark]
    public bool TypeCheck()
    {
        foreach (var node in _compilation.SyntaxTrees[0].GetRoot().DescendantNodes())
        {
            if (node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 })
            {
                return true;
            }
        }

        return false;
    }
}