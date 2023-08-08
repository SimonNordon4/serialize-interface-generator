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
        }

        public void ValidateClass()
        {
            if (!FieldDeclarations.Any()) return;
            
            var classGenerator = new ClassGenerator(this);
            classGenerator.GenerateClass();
        }
    }
}