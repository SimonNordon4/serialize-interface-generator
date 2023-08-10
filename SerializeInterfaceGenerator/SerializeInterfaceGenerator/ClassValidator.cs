using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializeInterfaceGenerator
{
    public readonly struct ClassValidator
    {
        public readonly GeneratorExecutionContext Context;
        public readonly ClassDeclarationSyntax ClassDeclaration;
        public readonly FieldDeclarationSyntax[] FieldDeclarations;
        
        public readonly List<UndefinedGenericParentInfo> UndefinedGenericParentFieldInfo;
   

        public ClassValidator(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            Context = context;
            ClassDeclaration = classDeclaration;
            

            SerializedInterfaceGenerator.CreateLog(DateTime.Now.ToString(CultureInfo.InvariantCulture), "verify run");
            
            FieldDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                .ToArray();

            
            // GENERIC TESTING.
            
            // FIRST CHECK IF PARENT IS GENERIC
            UndefinedGenericParentFieldInfo = new List<UndefinedGenericParentInfo>();
            
            // If no parent return;
            var baseType = classDeclaration.BaseList?.Types.FirstOrDefault();
            if (baseType == null) return;
            
            var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            
            var baseTypeInfo = semanticModel.GetTypeInfo(baseType.Type);
            if(!(baseTypeInfo.Type is INamedTypeSymbol baseNamedTypeSymbol)) return;

            if (!baseNamedTypeSymbol.IsGenericType) return;

            // Get the syntax reference for the parent class symbol
            var baseClassSyntaxRef = baseNamedTypeSymbol?.DeclaringSyntaxReferences.FirstOrDefault();
            var baseClassSyntaxNode = baseClassSyntaxRef?.GetSyntax();

            // Get all field declarations in the parent class
            var parentFieldDeclarations = baseClassSyntaxNode?.DescendantNodes().OfType<FieldDeclarationSyntax>().Where(f => f.AttributeLists.Any(
                a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface"))).ToArray() ?? Array.Empty<FieldDeclarationSyntax>();

            if(classDeclaration.Identifier.Text.Contains("Child"))
                SerializedInterfaceGenerator.CreateLog($"parent fields: {parentFieldDeclarations.Length}\n", classDeclaration.Identifier.Text);
            
            foreach (var parentField in parentFieldDeclarations)
            {
                var fieldType = parentField.Declaration.Type;
                var genericNameSyntax = fieldType.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault();
                
                if(genericNameSyntax == null) continue;
                
                if(classDeclaration.Identifier.Text.Contains("Child"))
                    SerializedInterfaceGenerator.AppendLog($"parent fields is generic", classDeclaration.Identifier.Text);
                
                var typeArguments = genericNameSyntax?.TypeArgumentList.Arguments;
                bool isUndefinedGenericField = false;
                foreach (var typeArgument in typeArguments)
                {
                    // If the field isn't undefined generic we skip.
                    if (!(typeArgument is IdentifierNameSyntax identifierType)) continue;
                    
                    // If it's an IdentifierNameSyntax and represents a generic type parameter, it will be part of the TypeParameterList
                    var containingType = identifierType.Ancestors().OfType<TypeDeclarationSyntax>()
                        .FirstOrDefault();
                    var containingMethod = identifierType.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

                    if (containingType?.TypeParameterList?.Parameters.Any(p =>
                            p.Identifier.Text == identifierType.Identifier.Text) == true
                        || containingMethod?.TypeParameterList?.Parameters.Any(p =>
                            p.Identifier.Text == identifierType.Identifier.Text) == true)
                    {
                        isUndefinedGenericField = true;
                    }
                }

                // We have detected that a field is a generic identifier, so we want to get the name of the field.

                if (!isUndefinedGenericField) continue;
                var fieldName = parentField.Declaration.Variables.First().Identifier.Text;
                var genericIdentifier = genericNameSyntax?.Identifier.Text;
                var originalGenericValue = baseNamedTypeSymbol?.TypeArguments.FirstOrDefault()?.Name;
                var originalGenericValueNamespace =
                    baseNamedTypeSymbol?.TypeArguments.FirstOrDefault()?.ContainingNamespace.Name;
                
                var originalGenericFullName = 
                    !string.IsNullOrEmpty(originalGenericValueNamespace) && originalGenericValueNamespace != "<global namespace>"
                        ? originalGenericValueNamespace + "." + originalGenericValue
                        : originalGenericValue;
                
                var genericInfo = new UndefinedGenericParentInfo(genericIdentifier, originalGenericFullName, fieldName);
                UndefinedGenericParentFieldInfo.Add(genericInfo);
            }
        }

        public void ValidateClass()
        {
            // TODO: check if generic.
            if (!FieldDeclarations.Any()) return;
            
            var classGenerator = new ClassGenerator(this);
            //classGenerator.GenerateClass();
        }

        public void ValidateClassOrReactiveSystem()
        {
            var baseType = ClassDeclaration.BaseList?.Types.FirstOrDefault()?.Type;
            var isBaseTypeReactiveSystem = baseType?.ToString().Contains("ReactiveSystem") ?? false;
            
            if (isBaseTypeReactiveSystem)
            {
                if (!(baseType is GenericNameSyntax genericBaseType)) return;

                // Get the type arguments as syntax nodes
                var typeArgumentSyntax = genericBaseType.TypeArgumentList.Arguments.First();
                var semanticModel = Context.Compilation.GetSemanticModel(typeArgumentSyntax.SyntaxTree);
                var typeSymbol = semanticModel.GetSymbolInfo(typeArgumentSyntax).Symbol;


                SerializedInterfaceGenerator.CreateLog(
                    $"BaseType: {baseType} \n "
                    + $"IsBaseTypeReactiveSystem: {isBaseTypeReactiveSystem} \n "
                    + $"Generic Value: {typeSymbol}"
                    , "IsIntSystem.txt");
                
                var reacitveClassGenerator = new ReactiveClassGenerator(this, typeSymbol,true);
                reacitveClassGenerator.GenerateClass();
            }

            
            ValidateClass();
            return;
        }
    }
}