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
        
        public readonly string GenericBaseFullName;
        public readonly List<UndefinedGenericParentInfo> UndefinedGenericParentFieldInfo;
   

        public ClassValidator(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            Context = context;
            ClassDeclaration = classDeclaration;

            FieldDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                .ToArray();

            # region Generic Parent Edge Case
            UndefinedGenericParentFieldInfo = new List<UndefinedGenericParentInfo>();
            GenericBaseFullName = "";
            
            if(classDeclaration.Identifier.Text.Contains("Child"))
                SerializedInterfaceGenerator.CreateLog($"\n", classDeclaration.Identifier.Text);
            
            #region Validate If Class is a Child of a Generic Parent
            
            // Check if there is a base type.
            // Class : Parent ?
            var baseType = classDeclaration.BaseList?.Types.FirstOrDefault(); 
            if (baseType == null) return;
            
            var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var baseTypeInfo = semanticModel.GetTypeInfo(baseType.Type);
            if(!(baseTypeInfo.Type is INamedTypeSymbol baseNamedTypeSymbol)) return;

            // Check if base type is generic.
            // Class : Parent<bool> ?
            if (!baseNamedTypeSymbol.IsGenericType) return; 
            
            #endregion
            
            
            // Get the namespace of the basetype.
            var baseTypeNamespace = baseNamedTypeSymbol.ContainingNamespace.ToString();
            
            // TODO: May also have to get the namespace of the generic type value.
            // MyNameSpace.Parent<bool> ->  MyNameSpace.Parent<bool>.Parent<System.Bool>
            GenericBaseFullName = !string.IsNullOrEmpty(baseTypeNamespace) && baseTypeNamespace != "<global namespace>"
                ? baseTypeNamespace + "." + baseType.ToString()
                : baseType.ToString();
            
            if(classDeclaration.Identifier.Text.Contains("Child"))
                SerializedInterfaceGenerator.AppendLog($"Base Type Full Name: {GenericBaseFullName}\n", classDeclaration.Identifier.Text);

            // Get the syntax reference for the parent class symbol
            var baseClassSyntaxRef = baseNamedTypeSymbol?.DeclaringSyntaxReferences.FirstOrDefault();
            var baseClassSyntaxNode = baseClassSyntaxRef?.GetSyntax();

            // Get all field declarations in the parent class
            // [SerializeInterface] private (any) m_Value;
            var parentFieldDeclarations = baseClassSyntaxNode?.DescendantNodes().OfType<FieldDeclarationSyntax>().Where(f => f.AttributeLists.Any(
                a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface"))).ToArray() ?? Array.Empty<FieldDeclarationSyntax>();

            if(classDeclaration.Identifier.Text.Contains("Child"))
                SerializedInterfaceGenerator.AppendLog($"parent fields: {parentFieldDeclarations.Length}\n", classDeclaration.Identifier.Text);
            
            foreach (var parentField in parentFieldDeclarations)
            {
                var fieldType = parentField.Declaration.Type;
                var genericNameSyntax = fieldType.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault();
                
                // [SerializeInterface] private T m_Value; ?
                // TODO: This wont be valid for T Values.
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
                
                // get the name of the generic itself.
                var genericIdentifier = genericNameSyntax?.Identifier.Text;
                var genericIdentifierNamespace = genericNameSyntax?.Ancestors().OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault()?.Name.ToString();
                var genericIdentifierFullName = 
                    !string.IsNullOrEmpty(genericIdentifierNamespace) && genericIdentifierNamespace != "<global namespace>"
                        ? genericIdentifierNamespace + "." + genericIdentifier
                        : genericIdentifier;
                
                // get the name of the generics actual value.
                var originalGenericValue = baseNamedTypeSymbol?.TypeArguments.FirstOrDefault()?.Name;
                var originalGenericValueNamespace =
                    baseNamedTypeSymbol?.TypeArguments.FirstOrDefault()?.ContainingNamespace.Name;
                var originalGenericValueFullName = 
                    !string.IsNullOrEmpty(originalGenericValueNamespace) && originalGenericValueNamespace != "<global namespace>"
                        ? originalGenericValueNamespace + "." + originalGenericValue
                        : originalGenericValue;
                
                // Now get the FULL GENERIC WITH VALUE
                var genericFullName = genericIdentifierFullName + "<" + originalGenericValueFullName + ">";
                
                var genericInfo = new UndefinedGenericParentInfo(fieldName, genericFullName);
                UndefinedGenericParentFieldInfo.Add(genericInfo);
                
                if(classDeclaration.Identifier.Text.Contains("Child"))
                    SerializedInterfaceGenerator.AppendLog($"genericInfo: {genericInfo}\n", classDeclaration.Identifier.Text);

            }
            #endregion
        }

        public void ValidateClass()
        {
            // If the class has no [SerializeInterface] fields AND it's parent has not [SerializeInterface] <T> fields, then return.
            if (!FieldDeclarations.Any() && !UndefinedGenericParentFieldInfo.Any()) return;
            
            var classGenerator = new ClassGenerator(this);
            //classGenerator.GenerateClass();
        }
    }
}