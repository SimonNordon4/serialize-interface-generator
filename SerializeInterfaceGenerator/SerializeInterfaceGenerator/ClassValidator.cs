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
        
        public ClassValidator(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            Context = context;
            ClassDeclaration = classDeclaration;
            FieldDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Any(
                    a => a.Attributes.Any(at => at.Name.ToString() == "SerializeInterface")))
                .ToArray();
            
            // Our class may inherit from a generic class, so we need to get the generic type arguments
            
            
            var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
            var baseTypeSymbol = classSymbol?.BaseType;
 
            var isDerivedFromGeneric = baseTypeSymbol != null && baseTypeSymbol.IsGenericType;
            
            SerializedInterfaceGenerator.PrintOutputToPath(
                $"{classDeclaration.Identifier.Text} is derived from generic: {isDerivedFromGeneric}", classDeclaration.Identifier.Text);
        }

        public void ValidateClass()
        {
            
        }
    }
}