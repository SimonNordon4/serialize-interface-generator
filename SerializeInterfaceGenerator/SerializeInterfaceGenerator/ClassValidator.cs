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