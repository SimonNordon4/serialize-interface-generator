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
    private const string AttributeText = @"
using System;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal class SerializeInterfaceAttribute : Attribute
{
}";
    
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization
            (i => i.AddSource("SerializeInterfaceAttribute_g.cs", AttributeText));
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        GenerateClasses(context);
    } 
    private static void GenerateClasses(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            return;

        foreach (var classDeclaration in receiver.Classes)
        {
            var interfaces = new HashSet<string>();

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
            source.AppendLine("using UnityEngine;");
            // This namespace contains our ValidateInterface Attribute and Drawer.
            source.AppendLine("using SerializeInterface;");
            
            // If the original class is a namespace, we want the generated class to be apart of the same namespace.
            if (!string.IsNullOrEmpty(namespaceName))
            {
                source.Append($@"namespace {namespaceName}");
                source.AppendLine("{");
            }
            
            source.AppendLine($"public partial class {classDeclaration.Identifier.Text} : MonoBehaviour, ISerializationCallbackReceiver");
            source.AppendLine("{");

            // Generate the concrete backing fields for each interface.
            foreach (var field in fields)
            {
                // Get the in script symbol for the field
                var symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;
                // Get the full name of the interface
                var interfaceNamespace = symbol.Type.ContainingNamespace.ToDisplayString();
                // If the interface is in a namespace, we need to include it in the full name
                var interfaceFullName =
                    !string.IsNullOrEmpty(interfaceNamespace) && interfaceNamespace != "<global namespace>"
                        ? interfaceNamespace + "." + symbol.Type.Name
                        : symbol.Type.Name;
                
                source.AppendLine(
                    $"    [SerializeField, ValidateInterface(typeof({interfaceFullName}))] private Object {field.Declaration.Variables.First().Identifier.Text}_Object;");
                
                interfaces.Add(interfaceFullName);
            }

            source.AppendLine("    void ISerializationCallbackReceiver.OnBeforeSerialize()");
            source.AppendLine("    {");
            //source.AppendLine("        OnBeforeSerialize();");
            source.AppendLine("    }");
            
            // We have to implement this because Unity didn't follow SOLID.
            source.AppendLine("    void ISerializationCallbackReceiver.OnAfterDeserialize()");
            source.AppendLine("    {");
            // Generate the code to assign the backing fields to the interface fields on BeforeSerialize
            foreach (var field in fields)
            {
                var symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;
                source.AppendLine(
                    $"        {field.Declaration.Variables.First().Identifier.Text} = {field.Declaration.Variables.First().Identifier.Text}_Object as {symbol.Type};");
            }
            source.AppendLine("    }");
            
            // Now add the InstantiateInterface method for each interface
            foreach (var @interface in interfaces)
            {
                // Generate the method for the interface
                source.AppendLine($"    private {@interface} InstantiateInterface({@interface} instance)");
                source.AppendLine("    {");
                source.AppendLine("        if (instance is MonoBehaviour monoInterface)");
                source.AppendLine($"            return Object.Instantiate(monoInterface) as {@interface};");
                source.AppendLine("        if (instance == null)");
                source.AppendLine("        {");
                source.AppendLine("            Debug.LogError(\"Attempted to instantiate interface with null instance!\", this);");
                source.AppendLine("            return null;");
                source.AppendLine("        }");
                source.AppendLine($"        Debug.LogError($\"Attempted to instantiate interface {@interface}, but it is not a MonoBehaviour!\", this);");
                source.AppendLine("        return null;");
                source.AppendLine("    }");
            }
            
            source.AppendLine("}");

            // If the original class was part of the namespace, we have to close off the scope.
            if (!string.IsNullOrEmpty(namespaceName))
            {
                source.AppendLine("}");
            }
            
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