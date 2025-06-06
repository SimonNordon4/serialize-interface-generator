﻿using System.Linq;
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
        private readonly FieldDeclarationSyntax[] _fieldDeclarations;
        private readonly string _classNameSpace;
        private readonly string _className;

        public ClassGenerator(ClassValidator validator)
        {
            _context = validator.Context;
            _semanticModel = validator.Context.Compilation.GetSemanticModel(validator.ClassDeclaration.SyntaxTree);
            _fieldDeclarations = validator.FieldDeclarations;
            _classNameSpace = validator.ClassDeclaration
                .AncestorsAndSelf()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString();
            _className = validator.ClassDeclaration.Identifier.Text;
        }


        public void GenerateClass()
        {
            var classSource = new StringBuilder();
            var backingFieldSource = new StringBuilder();
            var afterDeserializeSource = new StringBuilder();
            
            var indent = !string.IsNullOrEmpty(_classNameSpace) ? "    " : "";
            
            foreach (var field in _fieldDeclarations)
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
                $"{indent}public partial class {_className} : ISerializationCallbackReceiver");
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

            classSource.AppendLine($"{indent}}}");

            // If the original class was part of the namespace, we have to close off the scope.
            if (!string.IsNullOrEmpty(_classNameSpace))
            {
                classSource.AppendLine("}");
            }

            _context.AddSource($"{_className}_g.cs", SourceText.From(classSource.ToString(), Encoding.UTF8));
        }

        private bool DoesClassContainList()
        {
            foreach (var field in _fieldDeclarations)
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