using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializeInterfaceGenerator
{
    public readonly struct BackingFieldGenerator
    {
        private readonly ITypeSymbol _interfaceSymbol;
        private readonly INamedTypeSymbol _interfaceNamedTypeSymbol;

        // Flags
        private readonly bool _isStatic;
        private readonly bool _isReadOnly;
        private readonly bool _isInitialized;
        private readonly bool _isList;
        private readonly bool _isArray;
        private readonly bool _isGeneric;
        private readonly bool _isInterface;

        // Names
        private readonly string _originalFieldIdentifier;
        private readonly string _backingFieldIdentifier;
        private readonly string _interfaceFullName;

        // Attributes (if any).
        private readonly string _attributes;

        // Generic Values (if any).
        private readonly string _genericTypes;


        public BackingFieldGenerator(SemanticModel model, FieldDeclarationSyntax fieldDeclaration)
        {
            var fieldSymbol = model.GetDeclaredSymbol(fieldDeclaration.Declaration.Variables.First()) as IFieldSymbol;

            if (fieldSymbol == null) throw new NullReferenceException("Field symbol is null!");

            #region Get Flags

            // ---------------------------------------------------------------------------------------------------------

            _isList = fieldSymbol.Type.Name == "List";
            _isStatic = fieldSymbol.IsStatic;
            _isReadOnly = fieldSymbol.IsReadOnly;
            _isInitialized = fieldDeclaration.Declaration.Variables
                .Any(v => v.Initializer != null);
            _isList = fieldSymbol.Type.Name == "List";
            _isArray = fieldSymbol.Type.TypeKind == TypeKind.Array;

            var fieldNameTypeSymbol = fieldSymbol.Type as INamedTypeSymbol;

            // If it is a list, then the type is the first generic argument.
            _interfaceSymbol = _isList ? fieldNameTypeSymbol?.TypeArguments.First() : fieldSymbol.Type;
            _interfaceNamedTypeSymbol = _interfaceSymbol as INamedTypeSymbol;

            if (_interfaceSymbol == null) throw new NullReferenceException("Interface symbol is null!");

            _isInterface = _interfaceSymbol.TypeKind == TypeKind.Interface;
            _isGeneric = (_interfaceSymbol as INamedTypeSymbol)?.IsGenericType ?? false;

            #endregion

            # region Get Attributes

            // ---------------------------------------------------------------------------------------------------------

            var attributeBuilder = new StringBuilder();

            // Now get all attributes associated with the field, so that we can copy them to the backing field.
            foreach (var attributeList in fieldDeclaration.AttributeLists)
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
                            ? $"[{attributeName}]"
                            : $"[{attributeName}({arguments})]");
                }
            }

            _attributes = attributeBuilder.ToString();

            #endregion

            #region Get Generics

            // ---------------------------------------------------------------------------------------------------------
            
            _genericTypes = GenerateGenericTypeString(_interfaceNamedTypeSymbol);
            
            #endregion

            #region Get Names

            _originalFieldIdentifier = fieldDeclaration.Declaration.Variables.First().Identifier.Text;
            _backingFieldIdentifier = _originalFieldIdentifier + "Serialized";

            // Get the full name of the interface, including namespaces if any.
            var interfaceNamespace = _interfaceSymbol.ContainingNamespace.ToDisplayString();
            var interfaceFullName =
                !string.IsNullOrEmpty(interfaceNamespace) && interfaceNamespace != "<global namespace>"
                    ? interfaceNamespace + "." + _interfaceSymbol.Name
                    : _interfaceSymbol.Name;

            // If it's generic we need to add the generic values.
            if (_isGeneric) interfaceFullName = _genericTypes;
            _interfaceFullName = interfaceFullName;

            #endregion

            
            // Used for debugging purposes only.
            // PrintOutput(ToString());
        }


        public string GenerateBackingField(string indent = "")
        {
            if (!FieldCanBeSerialized()) return "";

            var serializedFieldType = _isList ? "List<UnityEngine.Object>" : "UnityEngine.Object";
            var interfaceToValidate = _isList ? _interfaceFullName + "[]" : _interfaceFullName;

            var fieldBuilder = new StringBuilder();
            fieldBuilder.AppendLine(
                $"{indent}    {_attributes}[SerializeField,ValidateInterface(typeof({_interfaceFullName}))]");
            fieldBuilder.AppendLine($"{indent}    private {serializedFieldType} {_backingFieldIdentifier};");
            return fieldBuilder.ToString();
        }

        public string GenerateOnAfterDeserialization(string indent)
        {
            if (!FieldCanBeSerialized()) return "";

            if (!_isList)
                return $"{indent}    {_originalFieldIdentifier} = {_backingFieldIdentifier} as {_interfaceFullName};\n";


            var listBuilder = new StringBuilder();
            if (!_isReadOnly)
            {
                listBuilder.AppendLine($"{indent}    if ({_originalFieldIdentifier} == null)");
                listBuilder.AppendLine(
                    $"{indent}        {_originalFieldIdentifier} = new List<{_interfaceFullName}>();");
            }

            listBuilder.AppendLine($"{indent}    {_originalFieldIdentifier}.Clear();");
            listBuilder.AppendLine($"{indent}    foreach (var obj in {_backingFieldIdentifier})");
            listBuilder.AppendLine($"{indent}        {_originalFieldIdentifier}.Add(obj as {_interfaceFullName});");
            return listBuilder.ToString();
        }

        private bool FieldCanBeSerialized()
        {
            // We only want to generate for interfaces.
            if (!_isInterface) return false;

            // Can't serialize statics (well we could if we wanted to... but shouldn't).
            if (_isStatic) return false;

            // Cannot serialize readonly fields.
            if (_isReadOnly && !_isList) return false;
            // Cannot serialize readonly list if they haven't been initialized.
            if (_isReadOnly && !_isInitialized) return false;

            return true;
        }

        private void PrintOutput(string toPrint)
        {
            var outputPath =
                $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\{_backingFieldIdentifier}_g.txt";
            System.IO.File.WriteAllText(outputPath, toPrint);
        }

        public override string ToString()
        {
            return $"InterfaceSymbol: {_interfaceSymbol},\n" +
                   $"NamedTypeSymbol: {_interfaceNamedTypeSymbol},\n" +
                   $"IsStatic: {_isStatic},\n" +
                   $"IsReadOnly: {_isReadOnly},\n" +
                   $"IsInitialized: {_isInitialized},\n" +
                   $"IsList: {_isList},\n" +
                   $"IsArray: {_isArray},\n" +
                   $"IsGeneric: {_isGeneric},\n" +
                   $"IsInterface: {_isInterface},\n" +
                   $"OriginalFieldIdentifier: {_originalFieldIdentifier},\n" +
                   $"BackingFieldIdentifier: {_backingFieldIdentifier},\n" +
                   $"InterfaceFullName: {_interfaceFullName},\n" +
                   $"Attributes: {_attributes},\n" +
                   $"GenericTypes: {_genericTypes}";
        }

        private static string GenerateGenericTypeString(INamedTypeSymbol namedTypeSymbol)
        {
            var genericTypes = new StringBuilder();

            var genericCount = namedTypeSymbol.TypeArguments.Length;
            if (namedTypeSymbol.IsGenericType && genericCount > 0)
            {
                genericTypes.Append("<");

                for (var i = 0; i < genericCount; i++)
                {
                    var typeSymbol = namedTypeSymbol.TypeArguments[i] as INamedTypeSymbol;

                    // If the typeSymbol is null or not a generic, we simply append the name
                    if (typeSymbol == null || !typeSymbol.IsGenericType)
                    {
                        var typeNameSpace = typeSymbol?.ContainingNamespace.ToDisplayString();
                        var typeFullName = !string.IsNullOrEmpty(typeNameSpace) && typeNameSpace != "<global namespace>"
                            ? typeNameSpace + "." + typeSymbol?.Name
                            : typeSymbol?.Name;

                        genericTypes.Append(i == genericCount - 1
                            ? typeFullName
                            : typeFullName + ",");
                    }
                    else
                    {
                        // If the typeSymbol is a generic, we call this method recursively to get nested generics
                        var nestedGeneric = GenerateGenericTypeString(typeSymbol);
                        genericTypes.Append(i == genericCount - 1
                            ? nestedGeneric
                            : nestedGeneric + ",");
                    }
                }

                genericTypes.Append(">");
            }

            // Generate the full name including the namespace and the generic types
            var symbolNamespace = namedTypeSymbol.ContainingNamespace.ToDisplayString();
            var symbolFullName = !string.IsNullOrEmpty(symbolNamespace) && symbolNamespace != "<global namespace>"
                ? symbolNamespace + "." + namedTypeSymbol.Name
                : namedTypeSymbol.Name;

            return symbolFullName + genericTypes;
        }
    }
}