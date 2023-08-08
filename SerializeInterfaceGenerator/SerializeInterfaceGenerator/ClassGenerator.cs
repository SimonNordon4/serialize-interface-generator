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
        private readonly FieldDeclarationSyntax[] _fieldDeclarations;
        private readonly string _classNameSpace;
        private readonly string _className;

        private readonly bool _printOutput;
        
        public ClassGenerator(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration, bool printOutput = false)
        {
            _context = context;
            _semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            
            
            _fieldDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                .ToArray();
            
            _classNameSpace = classDeclaration
                .AncestorsAndSelf()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString();
            
            _className = classDeclaration.Identifier.Text;
            
            _printOutput = printOutput;
        }

        public void GenerateClass()
        {
            if (!_fieldDeclarations.Any()) return;
            
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

        private void PrintOutput(string source)
        {
            var outputPath = $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\{_className}_g.txt";
           System.IO.File.WriteAllText(outputPath, source);
        }
        
    }
}