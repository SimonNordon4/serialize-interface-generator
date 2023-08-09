using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Security;
using SerializeInterfaceGenerator;

[Generator]
public class SerializedInterfaceGenerator : ISourceGenerator
{
//     private const string AttributeText = @"
// using System;
//
// [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
// internal class SerializeInterfaceAttribute : Attribute
// {
// }";

    public void Initialize(GeneratorInitializationContext context)
    {
        // context.RegisterForPostInitialization
        //     (i => i.AddSource("SerializeInterfaceAttribute_g.cs", AttributeText));
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            return;

        foreach (var classDeclaration in receiver.Classes)
        {
            var classValidator = new ClassValidator(context, classDeclaration);
            classValidator.ValidateClassOrReactiveSystem();
        }
    }
    


    public static void PrintOutputToPath(string source, string fileId)
    {
        var outputPath =
            $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\Log\{fileId}_g.txt";
        File.WriteAllText(outputPath, source.ToString());
    }

    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            // Identify classes that contain fields marked with the [SerializeInterface] attribute
            if (node is ClassDeclarationSyntax classDeclaration)
                Classes.Add(classDeclaration);
        }
    }
}