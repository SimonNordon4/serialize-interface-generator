using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SerializeInterfaceGenerator
{
    public readonly struct ClassGenerator
    {
        private readonly GeneratorExecutionContext _context;
        private readonly SemanticModel _semanticModel;
        private readonly FieldDeclarationSyntax[] _validFieldDeclarations;
        private readonly string _classNameSpace;
        private readonly string _className;
        private readonly bool _printOutput;

        public ClassGenerator(ClassValidator validator, bool printOutput = false)
        {
            _context = validator.Context;
            _semanticModel = validator.Context.Compilation.GetSemanticModel(validator.ClassDeclaration.SyntaxTree);
            _validFieldDeclarations = validator.FieldDeclarations;
            _classNameSpace = validator.ClassDeclaration
                .AncestorsAndSelf()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString();
            _className = validator.ClassDeclaration.Identifier.Text;
            _printOutput = printOutput;
        }

        public void GenerateClass()
        {
            var classSource = new StringBuilder();
            var backingFieldSource = new StringBuilder();
            var afterDeserializeSource = new StringBuilder();
            
            var indent = !string.IsNullOrEmpty(_classNameSpace) ? "    " : "";
            
            foreach (var field in _validFieldDeclarations)
            {
                // We don't want to serialize <T> fields. That will be done in the children.
                if(IsFieldIdentifierGeneric(field))
                    continue;
                
                var fieldGenerator = new BackingFieldGenerator(_semanticModel,field);
               backingFieldSource.Append(fieldGenerator.GenerateBackingField(indent));
               afterDeserializeSource.Append(fieldGenerator.GenerateOnAfterDeserialization(indent));
            }

            classSource.AppendLine("using UnityEngine;");
            // This namespace contains ValidateInterfaceAttribute, which is used to validate the interfaces.
            classSource.AppendLine("using SerializeInterface;");
            
            // If any of the fields are lists, we'll need to include this namespace.
            if (DoesClassContainList())
            {
                classSource.AppendLine("using System.Collections.Generic;");
            }

            // If the original class is a namespace, we want the generated class to be apart of the same namespace.
            if (!string.IsNullOrEmpty(_classNameSpace))
            {
                indent = "    ";
                classSource.AppendLine($@"namespace {_classNameSpace}");
                classSource.AppendLine("{");
            }

            classSource.AppendLine(
                $"{indent}public partial class {_className} : MonoBehaviour, ISerializationCallbackReceiver");
            classSource.AppendLine($"{indent}{{");

            // Add all the backing fields.
            classSource.Append("\n");
            classSource.Append(backingFieldSource);
            classSource.Append("\n");

            classSource.AppendLine($"{indent}    void ISerializationCallbackReceiver.OnBeforeSerialize()");
            classSource.AppendLine($"{indent}    {{");
            classSource.AppendLine($"{indent}    }}");
            classSource.Append("\n");
        

            // We have to implement this because Unity didn't follow SOLID.
            classSource.AppendLine($"{indent}    void ISerializationCallbackReceiver.OnAfterDeserialize()");
            classSource.AppendLine($"{indent}    {{");
            classSource.Append("\n");

            // Add all the deserialization logic.
            classSource.Append(afterDeserializeSource);
            
            classSource.Append("\n");
            classSource.AppendLine($"{indent}    }}");
            classSource.Append("\n");
            
            // Add all the instantiate methods to the bottom of the class.
            // classSource.Append(instantiateMethodSource);

            classSource.AppendLine($"{indent}}}");

            // If the original class was part of the namespace, we have to close off the scope.
            if (!string.IsNullOrEmpty(_classNameSpace))
            {
                classSource.AppendLine("}");
            }

            if(_printOutput) SerializedInterfaceGenerator.CreateLog(classSource.ToString(), _className);
            
            _context.AddSource($"{_className}_g.cs", SourceText.From(classSource.ToString(), Encoding.UTF8));
        }

        /// <summary>
        /// Determines whether or not a field is an Undefined Generic with Identifier. (i.e. IGeneric T)
        /// </summary>
        private static bool IsFieldIdentifierGeneric(FieldDeclarationSyntax field)
        {
            var fieldType = field.Declaration.Type;
            var genericNameSyntax = fieldType.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault();
            var typeArguments = genericNameSyntax?.TypeArgumentList.Arguments;

            if (typeArguments == null) return false;
            
            foreach (var typeArgument in typeArguments)
            {
                if (!(typeArgument is IdentifierNameSyntax identifierType)) continue;

                // If it's an IdentifierNameSyntax and represents a generic type parameter, it will be part of the TypeParameterList
                var containingType = identifierType.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                var containingMethod =
                    identifierType.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

                if (containingType?.TypeParameterList?.Parameters.Any(p =>
                        p.Identifier.Text == identifierType.Identifier.Text) == true
                    || containingMethod?.TypeParameterList?.Parameters.Any(p =>
                        p.Identifier.Text == identifierType.Identifier.Text) == true)
                {
                    return true;
                }
            }

            return false;
        }


        private bool DoesClassContainList()
        {
            foreach (var field in _validFieldDeclarations)
            {
                // check if field is a list
                if (field.Declaration.Type is GenericNameSyntax genericNameSyntax)
                {
                    if (genericNameSyntax.Identifier.Text == "List")
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        
    }
}