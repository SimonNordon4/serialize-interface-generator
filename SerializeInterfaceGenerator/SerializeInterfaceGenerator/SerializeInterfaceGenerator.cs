using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Linq;

[Generator]
public class SerializedInterfaceGenerator : ISourceGenerator
{
    private const string attributeText = @"
using System;
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class SerializeInterfaceAttribute : Attribute
{
    // This attribute doesn't need to contain any fields or properties
}
";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Check if the attribute is already defined
        // var attributeSymbol = context.Compilation.GetTypeByMetadataName("SerializeInterfaceAttribute");
        // if (attributeSymbol == null)
        // {
        //     // Add the attribute source code
        //     context.AddSource("SerializeInterfaceAttribute", SourceText.From(attributeText, Encoding.UTF8));
        // }

        // Check if any of the syntax trees in the compilation contain a using directive for "UnityEngine"
        bool isAssemblyCSharp = context.Compilation.SyntaxTrees.Any(tree =>
            tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Any(u => u.Name.ToString() == "UnityEngine"));

        if (isAssemblyCSharp)
        {
            // Add the attribute source code
            context.AddSource("SerializeInterfaceAttribute", SourceText.From(attributeText, Encoding.UTF8));
        }

        if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            return;


        foreach (var classDeclaration in receiver.Classes)
        {
            var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var fields = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")));

            if (!fields.Any())
                continue;

            var namespaceDeclaration = classDeclaration.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();
            var namespaceName = namespaceDeclaration?.Name.ToString();

            var source = new StringBuilder();
            if (!string.IsNullOrEmpty(namespaceName))
            {
                source.AppendLine($"namespace {namespaceName}");
                source.AppendLine("{");
            }

            source.AppendLine("using UnityEngine;");
            source.AppendLine(
                $"public partial class {classDeclaration.Identifier.Text} : MonoBehaviour, ISerializationCallbackReceiver");
            source.AppendLine("{");

            foreach (var field in fields)
            {
                var symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;
                var interfaceNamespace = symbol.Type.ContainingNamespace.ToDisplayString();
                var interfaceFullName = !string.IsNullOrEmpty(interfaceNamespace)
                    ? interfaceNamespace + "." + symbol.Type.Name
                    : symbol.Type.Name;
                source.AppendLine(
                    $"    [SerializeField, ValidateInterface(typeof({interfaceFullName}))] private Object {field.Declaration.Variables.First().Identifier.Text}_Object;");
            }


            source.AppendLine("    public void OnBeforeSerialize()");
            source.AppendLine("    {");

            foreach (var field in fields)
            {
                var symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;
                source.AppendLine(
                    $"        {field.Declaration.Variables.First().Identifier.Text} = {field.Declaration.Variables.First().Identifier.Text}_Object as {symbol.Type};");
            }

            source.AppendLine("    }");

            source.AppendLine("    public void OnAfterDeserialize()");
            source.AppendLine("    {");
            source.AppendLine("    }");

            source.AppendLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                source.AppendLine("}");
            }

            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "SG0001",
                    title: "Generated source code",
                    messageFormat: "Generated source code: {0}",
                    category: "SourceGenerator",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true),
                Location.None,
                source.ToString());
            context.ReportDiagnostic(diagnostic);

            context.AddSource($"{classDeclaration.Identifier.Text}_g.cs",
                SourceText.From(source.ToString(), Encoding.UTF8));
        }
    }

    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            // Identify classes that contain fields marked with the [SerializeInterface] attribute
            if (node is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .Any(f => f.AttributeLists.Count > 0))
            {
                Classes.Add(classDeclaration);
            }
        }
    }
}