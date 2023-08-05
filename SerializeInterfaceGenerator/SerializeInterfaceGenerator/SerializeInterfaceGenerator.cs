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
            var interfaces = new HashSet<string>();
            var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var fields = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                .ToArray();

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

            bool doAnyListsExist = false;

            foreach (var field in fields)
            {
                // Get the in script symbol for the field
                var symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;
                
                // Is this a readonly field?
                var isReadOnly = symbol?.IsReadOnly ?? false;
                
                // Check if the field is a list of interfaces
                var namedType = symbol?.Type as INamedTypeSymbol;
                var isList = namedType != null &&
                             namedType.IsGenericType &&
                             namedType.Name == "List" &&
                             namedType.TypeArguments.Length == 1 &&
                             namedType.TypeArguments[0].TypeKind == TypeKind.Interface;
                
                // We do not serialize readonly fields as they cannot be set.
                if(isReadOnly && !isList) continue;

                // If the field is a list, we want to get the type of the interface, not the list.
                var interfaceSymbol = isList
                    ? namedType.TypeArguments[0]
                    : symbol?.Type;
                
                // If the field is not an interface, or a list of interfaces, we skip it.
                if (interfaceSymbol?.TypeKind != TypeKind.Interface) continue;

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
                        
                        var attributeTypeSymbol = model.GetTypeInfo(attribute).Type;

                        // Get the arguments and convert them to string with correct format
                        var arguments = attribute.ArgumentList?.Arguments
                            .Select(arg => arg.ToString())
                            .Aggregate((a, b) => a + ", " + b);
                        
                        var attributeName = attributeTypeSymbol?.ToDisplayString() ?? attribute.Name.ToString();

                        attributeBuilder.AppendLine(
                            arguments == null
                                ? $"    [{attributeName}]"
                                : $"    [{attributeName}({arguments})]");
                    }
                }

                var backingFieldType = isList ? $"List<UnityEngine.Object>" : "UnityEngine.Object";
                var fieldDeclaration = field.Declaration.Variables.First().Identifier.Text;
                // Ideally we would call this _SerializedList, but because it's not possible to change the name of a list in the unity editor
                // This will at least make it appear as "Serialized My List" instead of something like "My List_SerializedObjectList".
                var suffix = isList ? "Serialized" : "SerializedObject";
                
                var backingFieldName = $"{fieldDeclaration}{suffix}";
                
                backingFieldsSource.Append(attributeBuilder);
                backingFieldsSource.AppendLine(
                    $"    [SerializeField,ValidateInterface(typeof({interfaceFullName}))] private {backingFieldType} {backingFieldName};");

                // Now create the Deserialize method.
                if (!isList)
                {
                    deserializationSource.AppendLine(
                        $"    {fieldDeclaration} = {backingFieldName} as {interfaceFullName};");
                }
                else
                {
                    deserializationSource.AppendLine($"    if ({fieldDeclaration} == null)");
                    deserializationSource.AppendLine("    {");

                    if (isReadOnly)
                    {
                        deserializationSource.AppendLine(
                            $@"        Debug.LogWarning(""[SerializeInterface] Cannot serialize the readonly list {fieldDeclaration} as it is null." + 
                            @" Please set it to a value in its declaration."",this);");
                        deserializationSource.AppendLine("    }");
                    }
                    else
                    {
                        deserializationSource.AppendLine($"        {fieldDeclaration} = new List<{interfaceFullName}>();");
                        deserializationSource.AppendLine("    }");
                        deserializationSource.AppendLine($"    else {fieldDeclaration}.Clear();");
                        deserializationSource.AppendLine($"    foreach (var obj in {backingFieldName})");
                        deserializationSource.AppendLine($"        {fieldDeclaration}.Add(obj as {interfaceFullName});");
                    }
                }
                
                if (isList) doAnyListsExist = true;

                interfaces.Add(interfaceFullName);
            }

            foreach (var @interface in interfaces)
            {
                // Generate the method for the interface
                instantiateMethodSource.AppendLine(
                    $"    private {@interface} InstantiateInterface({@interface} instance)");
                instantiateMethodSource.AppendLine("    {");
                instantiateMethodSource.AppendLine("        if (instance is MonoBehaviour monoInterface)");
                instantiateMethodSource.AppendLine(
                    $"            return Object.Instantiate(monoInterface) as {@interface};");
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
            
            // This namespace contains ValidateInterfaceAttribute, which is used to validate the interfaces.
            classSource.AppendLine("using SerializeInterface;");
            
            // If any of the fields are lists, we'll need to include this namespace.
            if (doAnyListsExist)
            {
                classSource.AppendLine("using System.Collections.Generic;");
            }

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

            //PrintOutputToPath(classSource, classDeclaration.Identifier.Text);

            context.AddSource($"{classDeclaration.Identifier.Text}_g.cs",
                SourceText.From(classSource.ToString(), Encoding.UTF8));
        }
    }

    private static void PrintOutputToPath(StringBuilder source, string fileId)
    {
        // var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        // var uri = new UriBuilder(codeBase);
        // var path = Uri.UnescapeDataString(uri.Path);
        // var assemblyDirectory = Path.GetDirectoryName(path);

        //var outputPath = Path.Combine(assemblyDirectory, $"Assets\\SerializeInterface\\{fileId}_g.txt");
        var outputPath =
            $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\{fileId}_g.txt";
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