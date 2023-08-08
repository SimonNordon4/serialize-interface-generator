using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializeInterfaceGenerator
{
    public readonly struct ClassValidator
    {
        public readonly GeneratorExecutionContext Context;
        public readonly ClassDeclarationSyntax ClassDeclaration;
        // public readonly FieldDeclarationSyntax[] FieldDeclarations;
        
        public ClassValidator(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            Context = context;
            ClassDeclaration = classDeclaration;
            FieldDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                .ToArray();
            
            
            // SPECIAL CASE FOR ReactiveSystem<T>.cs
            
            // GENERIC CLASS CHECK POINT.
            var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
            // check if the parent of the script is a ReactiveSystem
            var parentTypeSymbol = classSymbol?.BaseType;
            
            if (classDeclaration.Identifier.Text != "SpeedSystem") return;
            
            SerializedInterfaceGenerator
                .PrintOutputToPath($"Base type Symbol {parentTypeSymbol?.Name}\n"
                    ,classDeclaration.Identifier.Text);
            
            // // Check if the class is generic
            // var isClassGeneric = classSymbol?.IsGenericType ?? false;
            // if (isClassGeneric)
            // {
            //     // Filter out all fields that are undefined generics, we can't serialize IGeneric<T> fields
            //     FieldDeclarations = FilterOutUndefinedGenerics(semanticModel, FieldDeclarations);
            // }
            //     
            //
            //
            //
            //
            // var isDerivedFromGeneric = parentTypeSymbol != null && parentTypeSymbol.IsGenericType;
            //
            // var parentTypeArguments = parentTypeSymbol?.TypeArguments.First().Name;
            //
            // // Get all fields with SerializeInterface in the parent class that ARE undefined generics.
            // var parentFields = parentTypeSymbol?.GetMembers().Where(m => m.Kind == SymbolKind.Field)
            //     .Where(f => f.GetAttributes().Any(a => a.AttributeClass?.Name == "SerializeInterfaceAttribute"));
            //
            // var parentFieldsAsFieldDeclarations = parentFields?
            //     .Select(f => f.DeclaringSyntaxReferences.First().GetSyntax() as FieldDeclarationSyntax).ToArray();
            
        

        }
        
        private FieldDeclarationSyntax[] FilterOutUndefinedGenerics(SemanticModel model, FieldDeclarationSyntax[] fields)
        {
            return fields
                .Where(f => !IsUndefinedGeneric(model, f.Declaration.Type))
                .ToArray();
        }

        /// <summary>
        /// Detemines if a generic type is undefined.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="typeSyntax"></param>
        /// <returns></returns>
        private static bool IsUndefinedGeneric(SemanticModel model, TypeSyntax typeSyntax)
        {
            if (typeSyntax is GenericNameSyntax genericName)
            {
                foreach (var argument in genericName.TypeArgumentList.Arguments)
                {
                    if (argument is IdentifierNameSyntax identifier &&
                        model.GetSymbolInfo(identifier).Symbol is ITypeParameterSymbol)
                    {
                        return true; // Generic argument is an undefined type parameter
                    }
                }
            }

            return false;
        }
        

        public void ValidateClass()
        {
            // SerializedInterfaceGenerator
            //     .PrintOutputToPath($"Number of Fields: {FieldDeclarations.Length}\n"
            //         ,ClassDeclaration.Identifier.Text);
        }
    }
}