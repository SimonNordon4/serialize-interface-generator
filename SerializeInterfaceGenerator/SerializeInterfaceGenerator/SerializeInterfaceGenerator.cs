using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Linq;
using System.Reflection;

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
            GenerateClass(context, classDeclaration);
        }
    }

    /// <summary>
    /// Generate a partial class when detecting a [SerializeInterface]
    /// </summary>
    private static void GenerateClass(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
    {
        HashSet<string> interfaces = new HashSet<string>();

        SemanticModel model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);

        FieldDeclarationSyntax[] fields = classDeclaration
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f =>
                f.AttributeLists
                    .Any(a =>
                        a.Attributes
                            .Any(at => at.Name.ToString() == "SerializeInterface")))
            .ToArray();

        // If no [SerializeInterface] attribute is found, skip this class.
        if (!fields.Any()) return;

        NamespaceDeclarationSyntax namespaceDeclaration = classDeclaration
            .AncestorsAndSelf()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();
        string namespaceName = namespaceDeclaration?.Name.ToString();

        var source = new StringBuilder();

        source.AppendLine("using UnityEngine;");
        source.AppendLine("using SerializeInterface;");

        // If the original class is a namespace, we want the generated class to be apart of the same namespace.
        if (!string.IsNullOrEmpty(namespaceName))
        {
            source.AppendLine($@"namespace {namespaceName}");
            source.AppendLine("{");
        }

        source.AppendLine(
            $"public partial class {classDeclaration.Identifier.Text} : MonoBehaviour, ISerializationCallbackReceiver");
        source.AppendLine("{");

        // Generate the concrete backing fields for each interface.
        foreach (var field in fields)
        {
            // Get the in script symbol for the field
            IFieldSymbol symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;

            // Check if the field is a list of interfaces
            bool isList = symbol?.Type is INamedTypeSymbol namedType &&
                          namedType.IsGenericType &&
                          namedType.Name == "List" &&
                          namedType.TypeArguments.Length == 1 &&
                          namedType.TypeArguments[0].TypeKind == TypeKind.Interface;


            source.Append(
                isList
                    ? GenerateBackingObjectList(field, symbol)
                    : GenerateBackingObject(field, symbol));


            interfaces.Add(interfaceFullName);
        }

        source.AppendLine("    void ISerializationCallbackReceiver.OnBeforeSerialize()");
        source.AppendLine("    {");
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
            source.AppendLine(
                "            Debug.LogError(\"Attempted to instantiate interface with null instance!\", this);");
            source.AppendLine("            return null;");
            source.AppendLine("        }");
            source.AppendLine(
                $"        Debug.LogError($\"Attempted to instantiate interface {@interface}, but it is not a MonoBehaviour!\", this);");
            source.AppendLine("        return null;");
            source.AppendLine("    }");
        }

        source.AppendLine("}");

        // If the original class was part of the namespace, we have to close off the scope.
        if (!string.IsNullOrEmpty(namespaceName))
        {
            source.AppendLine("}");
        }

        // PrintOutputToPath(source, classDeclaration.Identifier.Text);

        context.AddSource($"{classDeclaration.Identifier.Text}_g.cs",
            SourceText.From(source.ToString(), Encoding.UTF8));
    }


    /// <summary>
    /// Generate a back object to match an Interface.
    /// </summary>
    /// <returns></returns>
    private static string GenerateBackingObject(FieldDeclarationSyntax field, IFieldSymbol symbol)
    {
        var interfaceFullName = GetInterfaceFullName(symbol);

        // Gather all attributes on the original [SerializeInterface]
        StringBuilder attributeStringBuilder = new StringBuilder();

        // Loop through each attribute list in the field declaration
        foreach (var attributeList in field.AttributeLists)
        {
            var inheritedAttributeList = GenerateInheritedAttributes(attributeList);
            attributeStringBuilder.Append(inheritedAttributeList);
        }

        return
            $"    [SerializeField, ValidateInterface(typeof({interfaceFullName}))]{attributeStringBuilder} private Object {field.Declaration.Variables.First().Identifier.Text}_SerializedObject;";
    }

    /// <summary>
    /// Generate a backing Object List to match an Interface List.
    /// </summary>
    /// <returns></returns>
    private static string GenerateBackingObjectList(FieldDeclarationSyntax field, IFieldSymbol symbol)
    {
        var interfaceFullName = GetInterfaceFullName(symbol);

        // Gather all attributes on the original [SerializeInterface]
        StringBuilder attributeStringBuilder = new StringBuilder();

        // Loop through each attribute list in the field declaration
        foreach (var attributeList in field.AttributeLists)
        {
            var inheritedAttributeList = GenerateInheritedAttributes(attributeList);
            attributeStringBuilder.Append(inheritedAttributeList);
        }

        return
            $"    [SerializeField, ValidateInterface(typeof({interfaceFullName}))]{attributeStringBuilder} private List<Object> {field.Declaration.Variables.First().Identifier.Text}_SerializedObjectList;";
    }

    /// <summary>
    /// Generates a string representation of inherited attributes from a given attribute list.
    /// </summary>
    /// <param name="attributeList">The list of attributes to process, e.g. [Range(), Tooltip].</param>
    /// <returns>A string representation of inherited attributes.</returns>
    private static string GenerateInheritedAttributes(AttributeListSyntax attributeList)
    {
        var inheritedAttributesList = new StringBuilder();
    
        // Loop through each attribute in the attribute list
        foreach (var attribute in attributeList.Attributes)
        {
            var inheritedAttribute = GenerateInheritedAttributes(attribute);
            inheritedAttributesList.Append(inheritedAttribute);
        }

        return inheritedAttributesList.ToString();
    }

    private static string GenerateInheritedAttributes(AttributeSyntax attribute)
    {
        // If the attribute is not SerializeInterface, append it to the attribute string builder
        if (attribute.Name.ToString() == "SerializeInterface") return "";

        // Get the arguments and convert them to string with correct format
        var arguments = attribute.ArgumentList?.Arguments
            .Select(arg => arg.ToString())
            .Aggregate((a, b) => a + ", " + b);

        return arguments == null ? $"[{attribute.Name}]" : $"[{attribute.Name}({arguments})]";
    }

    /// <summary>
    /// Generate method for Deserializing the backing Object.
    /// </summary>
    /// <returns></returns>
    private static string GenerateObjectDeserialization()
    {
        return null;
    }

    /// <summary>
    /// Generate method for Deserializing the backing Object List.
    /// </summary>
    /// <returns></returns>
    private static string GenerateListDeserialization()
    {
        return null;
    }

    /// <summary>
    /// Generate a method for instantiating an interface that is assumed to be a mono behaviour.
    /// </summary>
    /// <returns></returns>
    private static string GenerateInterfaceInstantiateMethod()
    {
        return null;
    }

    /// <summary>
    /// Generate the the full name of the symbol, including optional namespace.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    private static string GetInterfaceFullName(IFieldSymbol symbol)
    {
        // Get the full name of the interface
        string interfaceNamespace = symbol?.Type.ContainingNamespace.ToDisplayString();
        // If the interface is in a namespace, we need to include it in the full name
        string interfaceFullName =
            !string.IsNullOrEmpty(interfaceNamespace) && interfaceNamespace != "<global namespace>"
                ? interfaceNamespace + "." + symbol.Type.Name
                : symbol?.Type.Name;

        return interfaceFullName;
    }


    private static void PrintOutputToPath(StringBuilder source, string classId)
    {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        var assemblyDirectory = Path.GetDirectoryName(path);

        var outputPath = Path.Combine(assemblyDirectory, $"Assets\\SerializeInterface\\{classId}_g.txt");
        File.WriteAllText(outputPath, source.ToString());
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