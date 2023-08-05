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
        GenerateClassesOld(context);
    }

    private static void GenerateClassesOld(GeneratorExecutionContext context)
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

            source.AppendLine("    public void OnBeforeSerialize()");
            source.AppendLine("    {");
            source.AppendLine("    }");
            
            // We have to implement this because Unity didn't follow SOLID.
            source.AppendLine("    public void OnAfterDeserialize()");
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
        var interfaces = new HashSet<string>();
        var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
        var fields = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(f => f.AttributeLists.Any(
                a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")));

        // If no [SerializeInterface] attribute is found, skip this class.
        if (!fields.Any()) return;

        var namespaceDeclaration = classDeclaration
            .AncestorsAndSelf()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();
        var namespaceName = namespaceDeclaration?.Name.ToString();
        

        var backingFieldsSource = new StringBuilder();
        var deserializationSource = new StringBuilder();
        var instantiateMethodSource = new StringBuilder();
        
        // The main class source builder
        var classSource = new StringBuilder();
        
        foreach (var field in fields)
        {
            // Get the in script symbol for the field
            var symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;

            // Check if the field is a list of interfaces
            var namedType = symbol?.Type as INamedTypeSymbol;
            var isList = namedType != null &&
                         namedType.IsGenericType &&
                         namedType.Name == "List" &&
                         namedType.TypeArguments.Length == 1 &&
                         namedType.TypeArguments[0].TypeKind == TypeKind.Interface;

            // If the field is a list, we want to get the type of the interface, not the list.
            var interfaceSymbol = isList
                ? namedType.TypeArguments[0]
                : symbol?.Type;

            // Get the full name of the interface, including namespaces if any.
            var interfaceNamespace = interfaceSymbol?.ContainingNamespace.ToDisplayString();
            var interfaceFullName =
                !string.IsNullOrEmpty(interfaceNamespace) && interfaceNamespace != "<global namespace>"
                    ? interfaceNamespace + "." + interfaceSymbol.Name
                    : interfaceSymbol?.Name;
            
            var attributeBuilder = new StringBuilder();
            // Now get all attributes associated with the field, so that we can copy them to the backing field.
            foreach (var attributeList in field.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // If the attribute is not SerializeInterface, append it to the attribute string builder
                    if (attribute.Name.ToString() == "SerializeInterface") continue;

                    // Get the arguments and convert them to string with correct format
                    var arguments = attribute.ArgumentList?.Arguments
                        .Select(arg => arg.ToString())
                        .Aggregate((a, b) => a + ", " + b);

                    attributeBuilder.Append(
                        arguments == null
                        ? $"[{attribute.Name}]"
                        : $"[{attribute.Name}({arguments})]");
                }
            }
            
            var backingFieldType = isList ? $"List<UnityEngine.Object>" : "UnityEngine.Object";
            var fieldDeclaration = field.Declaration.Variables.First().Identifier.Text;
            var suffix = isList ? "ObjectList" : "Object";
            backingFieldsSource.AppendLine(
                $"        [SerializeField] private {backingFieldType} {fieldDeclaration}_{suffix};");
            
            // Now create the Deserialize method.
            if (!isList)
            {
                deserializationSource.AppendLine(
                    $"        {fieldDeclaration} = {fieldDeclaration}_{suffix} as {interfaceSymbol};");
            }
            else
            {
                deserializationSource.AppendLine($"            if ({fieldDeclaration} == null) {fieldDeclaration} = new List<{interfaceSymbol}>();");
                deserializationSource.AppendLine($"            else {fieldDeclaration}.Clear();");
                deserializationSource.AppendLine($"            foreach (var obj in {fieldDeclaration}_{suffix})");
                deserializationSource.AppendLine($"                {fieldDeclaration}.Add(obj as {interfaceSymbol});");
            }

            interfaces.Add(interfaceFullName);
        }

        foreach (var @interface in interfaces)
        {
            // Generate the method for the interface
            instantiateMethodSource.AppendLine($"    private {@interface} InstantiateInterface({@interface} instance)");
            instantiateMethodSource.AppendLine("    {");
            instantiateMethodSource.AppendLine("        if (instance is MonoBehaviour monoInterface)");
            instantiateMethodSource.AppendLine($"            return Object.Instantiate(monoInterface) as {@interface};");
            instantiateMethodSource.AppendLine("        if (instance == null)");
            instantiateMethodSource.AppendLine("        {");
            instantiateMethodSource.AppendLine(
                "            Debug.LogError(\"Attempted to instantiate interface with null instance!\", this);");
            instantiateMethodSource.AppendLine("            return null;");
            instantiateMethodSource.AppendLine("        }");
            instantiateMethodSource.AppendLine(
                $"        Debug.LogError($\"Attempted to instantiate interface {@interface}, but it is not a MonoBehaviour!\", this);");
            instantiateMethodSource.AppendLine("        return null;");
            instantiateMethodSource.AppendLine("    }");
        }

        classSource.AppendLine("using UnityEngine;");
        classSource.AppendLine("using SerializeInterface;");

        // If the original class is a namespace, we want the generated class to be apart of the same namespace.
        if (!string.IsNullOrEmpty(namespaceName))
        {
            classSource.AppendLine($@"namespace {namespaceName}");
            classSource.AppendLine("{");
        }

        classSource.AppendLine(
            $"public partial class {classDeclaration.Identifier.Text} : MonoBehaviour, ISerializationCallbackReceiver");
        classSource.AppendLine("{");

        // Add all the backing fields.
        classSource.Append(backingFieldsSource);

        classSource.AppendLine("    void ISerializationCallbackReceiver.OnBeforeSerialize()");
        classSource.AppendLine("    {");
        classSource.AppendLine("    }");

        // We have to implement this because Unity didn't follow SOLID.
        classSource.AppendLine("    void ISerializationCallbackReceiver.OnAfterDeserialize()");
        classSource.AppendLine("    {");

        // Add all the deserialization logic.
        classSource.Append(deserializationSource);

        classSource.AppendLine("    }");

        // Add all the instantiate methods to the bottom of the class.
        classSource.Append(instantiateMethodSource);

        classSource.AppendLine("}");

        // If the original class was part of the namespace, we have to close off the scope.
        if (!string.IsNullOrEmpty(namespaceName))
        {
            classSource.AppendLine("}");
        }

        PrintOutputToPath(classSource, classDeclaration.Identifier.Text);

        context.AddSource($"{classDeclaration.Identifier.Text}_g.cs",
            SourceText.From(classSource.ToString(), Encoding.UTF8));
    }

    private static void PrintOutputToPath(StringBuilder source, string classId)
    {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        var assemblyDirectory = Path.GetDirectoryName(path);

        var outputPath = Path.Combine(assemblyDirectory, $"Assets\\SerializeInterface\\{classId}_g.txt");
        outputPath = @"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets";
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