﻿using System;
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
        private readonly IFieldSymbol[] _genericParentFields;
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

            if (validator.IsParentClassGeneric)
            {
                _genericParentFields = validator.ParentNamedTypeSymbol?.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(member => member.GetAttributes().Any(at => at.AttributeClass.Name == "SerializeInterface") &&
                                     member.Type is INamedTypeSymbol namedType && namedType.IsGenericType)
                    .ToArray() ?? Array.Empty<IFieldSymbol>();
            }

            _genericParentFields = Array.Empty<IFieldSymbol>();

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

            if(_printOutput) PrintOutput(classSource.ToString());
            
            _context.AddSource($"{_className}_g.cs", SourceText.From(classSource.ToString(), Encoding.UTF8));
        }

        private void GenerateClassWithGenericParent()
        {
            // // check if the parent of the class is a generic class
            // // if it is, we need to add the generic type to the class name
            // var parentClassDeclaration = _classDeclaration
            //     .AncestorsAndSelf()
            //     .OfType<ClassDeclarationSyntax>()
            //     .Skip(1)
            //     .FirstOrDefault();
            //
            // if (parentClassDeclaration?.TypeParameterList?.Parameters.Count > 0)
            // {
            //     var parentGenericType = parentClassDeclaration.TypeParameterList.Parameters.First();
            //     
            //     // Get all parent fields that are marked with the SerializeInterface attribute, generic and use the same argument as the parent.
            //     _parentFieldDeclarations = parentClassDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
            //         .Where(f => f.AttributeLists.Any(
            //                         a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface"))
            //                     && f.Declaration.Type is GenericNameSyntax genericType
            //                     && genericType.TypeArgumentList.Arguments.Any(arg => arg.ToString() == parentGenericType.Identifier.Text)) // Compare with parent's generic type
            //         .ToArray();
            //     
            //     _parentIsGenericWithGenericFields = _parentFieldDeclarations.Any();
            // }
            // else
            // {
            //     _parentFieldDeclarations = Array.Empty<FieldDeclarationSyntax>();
            //     _parentIsGenericWithGenericFields = false;
            //}
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

        private void PrintOutput(string source)
        {
            var outputPath = $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\{_className}_g.txt";
           System.IO.File.WriteAllText(outputPath, source);
        }
        
    }
}