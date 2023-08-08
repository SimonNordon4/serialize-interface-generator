using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SerializeInterfaceGenerator
{
    public readonly struct ClassGenerator
    {
        private readonly GeneratorExecutionContext _context;
        private readonly SemanticModel _semanticModel;
        private readonly ClassDeclarationSyntax _classDeclaration;
        private readonly FieldDeclarationSyntax[] _fieldDeclarations;
        private readonly string _classNameSpace;
        private readonly string _className;

        private readonly INamedTypeSymbol _classSymbol;
        private readonly bool _isGeneric;

        private readonly bool _printOutput;
        
        public ClassGenerator(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration, SemanticModel model, FieldDeclarationSyntax[] fields, bool printOutput = false)
        {
            _context = context;
            _semanticModel = model;
            _fieldDeclarations = fields;
            _classDeclaration = classDeclaration;
            _classSymbol = _semanticModel.GetDeclaredSymbol(_classDeclaration);
            _isGeneric = _classSymbol?.IsGenericType ?? false;

            _classNameSpace = classDeclaration
                .AncestorsAndSelf()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString();

            _className = classDeclaration.Identifier.Text;

            _printOutput = printOutput;
            
        }

        private static bool IsDerivedFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseTypeSymbol)
        {
            while (typeSymbol != null)
            {
                if (typeSymbol.Equals(baseTypeSymbol))
                {
                    return true;
                }

                typeSymbol = typeSymbol.BaseType;
            }

            return false;
        }
        
        private static List<INamedTypeSymbol> GetAllDerivedClasses(INamedTypeSymbol baseType, Compilation compilation)
        {
            var derivedClasses = new List<INamedTypeSymbol>();

            // Iterate over all trees in the compilation
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
        
                // Iterate over all class declarations in the tree
                foreach (var classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                    if (classSymbol != null && IsDerivedFrom(classSymbol, baseType))
                    {
                        derivedClasses.Add(classSymbol);
                    }
                }
            }

            return derivedClasses;
        }
        
        public void GenerateClass()
        {
            if (!_fieldDeclarations.Any()) return;

            if (_isGeneric)
            {
                var derivedTypes = new List<INamedTypeSymbol>();
                
                var _compilation = _semanticModel.Compilation;
                var targetType = _semanticModel.GetTypeInfo(_classDeclaration).Type;
                
                foreach (var syntaxTree in _compilation.SyntaxTrees)
                {
                    var semanticModel = _compilation.GetSemanticModel(syntaxTree);
                    var classDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
                    
                    PrintOutput(classDeclarations.Count().ToString(), _className + " Class Declarations");

                    foreach (var classDeclaration in classDeclarations)
                    {
                        var classSymbol = semanticModel.GetDeclaredSymbol(_classDeclaration) as INamedTypeSymbol;
                        var currentSymbol = classSymbol.BaseType;

                        while (currentSymbol != null)
                        {
                            if (currentSymbol.Equals(targetType))
                            {
                                derivedTypes.Add(classSymbol);
                                break;
                            }

                            currentSymbol = currentSymbol.BaseType;
                        }
                    }
                }
            }

            
            
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

            if(_printOutput) PrintOutput(classSource.ToString(), _className);
            
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

        private void PrintOutput(string source, string fileName)
        {
            var outputPath = $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\{fileName}_g.txt";
           System.IO.File.WriteAllText(outputPath, source);
        }
        
    }
}