using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializeInterfaceGenerator
{
    public readonly struct ClassValidator
    {
        public readonly GeneratorExecutionContext Context;
        public readonly ClassDeclarationSyntax ClassDeclaration;
        public readonly FieldDeclarationSyntax[] FieldDeclarations;
        public readonly bool IsParentClassGeneric;
        public readonly INamedTypeSymbol ParentNamedTypeSymbol;
   

        public ClassValidator(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            Context = context;
            ClassDeclaration = classDeclaration;
            
            if (classDeclaration.Identifier.Text.Contains("A_Child"))
                SerializedInterfaceGenerator.PrintOutputToPath(DateTime.Now.ToString(CultureInfo.InvariantCulture), "verify run");
            
            FieldDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                .ToArray();

            
            var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            
            // CHECK IF UNDEFINED GENERICS IN FIELDS
            /*foreach (var field in FieldDeclarations)
            {
                var fieldType = field.Declaration.Type;
                var typeInfo = semanticModel.GetTypeInfo(fieldType);
                var namedTypeSymbol = typeInfo.Type as INamedTypeSymbol;

                if (namedTypeSymbol != null && namedTypeSymbol.IsGenericType)
                {
                    // Check if any of the generic type arguments are undefined or using some placeholder
                    if (namedTypeSymbol.TypeArguments.Any(arg => )// Condition to check if the argument is undefined))
                    {
                        // Handle the undefined generic field
                    }
                }
                
            }*/
            
            //Everything below wrapped in a single arg lol.
            //var isUndefinedGenericField = (classDeclaration.BaseList?.Types.FirstOrDefault() as INamedTypeSymbol).DeclaringSyntaxReferences.FirstOrDefault().GetSyntax().DescendantNodes().OfType<FieldDeclarationSyntax>().ToArray().Where(f => f.AttributeLists.Any(a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface"))).First().Declaration.Type.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault().TypeArgumentList.Arguments.ToArray().OfType<IdentifierNameSyntax>().Where(identifierType => identifierType.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.TypeParameterList?.Parameters.Any(p => p.Identifier.Text == identifierType.Identifier.Text) == true || identifierType.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.TypeParameterList?.Parameters.Any(p => p.Identifier.Text == identifierType.Identifier.Text) == true).Any();

            
            var baseType = classDeclaration.BaseList?.Types.FirstOrDefault();
            if (baseType != null)
            {
                var baseTypeInfo = semanticModel.GetTypeInfo(baseType.Type);
                ParentNamedTypeSymbol = baseTypeInfo.Type as INamedTypeSymbol;
                IsParentClassGeneric = ParentNamedTypeSymbol != null && ParentNamedTypeSymbol.IsGenericType;
            }
            else
            {
                IsParentClassGeneric = false;
                ParentNamedTypeSymbol = null;
            }
            
            // Get the syntax reference for the parent class symbol
            var baseClassSyntaxRef = ParentNamedTypeSymbol?.DeclaringSyntaxReferences.FirstOrDefault();
            var baseClassSyntaxNode = baseClassSyntaxRef?.GetSyntax();

            // Get all field declarations in the parent class
            var parentFieldDeclarations = baseClassSyntaxNode?.DescendantNodes().OfType<FieldDeclarationSyntax>().ToArray() ?? Array.Empty<FieldDeclarationSyntax>();
            
            if (classDeclaration.Identifier.Text.Contains("A_Child"))
            {
            
                    SerializedInterfaceGenerator
                    .PrintOutputToPath($"Is parent generic? {IsParentClassGeneric}" +
                                                               $"parent class: {ParentNamedTypeSymbol}\n " +
                                                               $"parent fields: {parentFieldDeclarations.Length}\n "
                    ,classDeclaration.Identifier.Text);
                
                    // Select all fields in parentFieldDeclarations that have a SerializeInterface attribute
                    var validParentSerializeInterfaceFields = parentFieldDeclarations
                        .Where(f => f.AttributeLists.Any(
                            a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                        .ToArray();
                    
                    SerializedInterfaceGenerator
                        .PrintOutputToPath($"valid field {validParentSerializeInterfaceFields.Length}"
                            ,classDeclaration.Identifier.Text + "_2");

                    var singleField = validParentSerializeInterfaceFields.First();
                    var fieldType = singleField.Declaration.Type;
                    var fieldTypeArgs = fieldType.ChildNodes().OfType<GenericNameSyntax>().ToArray();
                    var genericNameSyntax = fieldType.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault();
                    
                    var typeArguments = genericNameSyntax?.TypeArgumentList.Arguments;

                    var args = "";
                    foreach (var typeArgument in typeArguments)
                    {
                        if (typeArgument is IdentifierNameSyntax identifierType)
                        {
                            // If it's an IdentifierNameSyntax and represents a generic type parameter, it will be part of the TypeParameterList
                            var containingType = identifierType.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                            var containingMethod = identifierType.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

                            if (containingType?.TypeParameterList?.Parameters.Any(p => p.Identifier.Text == identifierType.Identifier.Text) == true
                                || containingMethod?.TypeParameterList?.Parameters.Any(p => p.Identifier.Text == identifierType.Identifier.Text) == true)
                            {
                                // The identifier represents a generic type parameter (e.g., <T>)
                                // Handle this case
                                args += "generic type parameter";
                            }
                            else
                            {
                                // The identifier represents a custom class or struct
                                // Handle this case
                                args += "custom class or struct";
                            }
                        }
                    }
                    
                    
                    SerializedInterfaceGenerator
                        .PrintOutputToPath($"field type? {singleField.Declaration.Type}\n " +
                                           $"field type args {singleField.Declaration.Type}\n " +
                                           $"genericNameSyntax {genericNameSyntax}\n " +
                                           $"Type Arguments {typeArguments}\n " +
                                           $"Type Argument Type {args}\n " 
                            ,classDeclaration.Identifier.Text + "_3");

            }
        }

        public void ValidateClass()
        {
            if (!FieldDeclarations.Any() && !IsParentClassGeneric) return;
            
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


                SerializedInterfaceGenerator.PrintOutputToPath(
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