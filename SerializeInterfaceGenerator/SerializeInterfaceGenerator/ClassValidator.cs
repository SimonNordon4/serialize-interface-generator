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
            
            var baseTypeArgument = baseTypeSymbol?.TypeArguments.First().Name;
            
            // Get all fields with SerializeInterface in the parent class
            var parentFields = baseTypeSymbol?.GetMembers().Where(m => m.Kind == SymbolKind.Field)
                .Where(f => f.GetAttributes().Any(a => a.AttributeClass?.Name == "SerializeInterfaceAttribute"));

            if (classDeclaration.Identifier.Text == "Child")
            {
                SerializedInterfaceGenerator.PrintOutputToPath("Child Found", "ChildFound");
                SerializedInterfaceGenerator
                    .PrintOutputToPath($"Is derived from generic? {isDerivedFromGeneric}\n" +
                    $"Base Arguments {baseTypeArgument}\n" +
                    $"Base type Symbol {baseTypeSymbol?.Name}\n" +
                    $"Number of fields in the parent class {parentFields?.Count()}\n",
                        "Child_Parent");
            }

        }

        public void ValidateClass()
        {
            
        }
    }
}