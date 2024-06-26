using AutoProperty.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AutoProperty.Tests;

public class AutoPropertyGeneratorTests
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

    [Fact]
    public void GenerateClassWithInterface()
    {
        // Create an instance of the source generator.
        var generator = new AutoPropertyGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        // Create a new instance of our generator, and pass it to the driver
        var driver = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(nameof(GenerateClassWithInterface),
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

        // Run generators and retrieve all results.
        var result = driver.RunGenerators(compilation)
            .GetRunResult();

        // All generated files can be found in 'RunResults.GeneratedTrees'.
        Assert.Collection(result.GeneratedTrees, tree =>
        {
            // Validate the generated file has expected output and filename
            Assert.Equal("Book.g.cs", Path.GetFileName(tree.FilePath));
            Assert.Equal("// Generated code for Book", tree.GetText().ToString());
        });
    }
}