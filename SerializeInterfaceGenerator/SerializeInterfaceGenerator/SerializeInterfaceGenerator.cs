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

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            return;

        foreach (var classDeclaration in receiver.Classes)
        {
            var classValidator = new ClassValidator(context, classDeclaration);
            classValidator.ValidateClass();
        }
    }

    public static void AppendLog(string source, string fileId)
    {
        var outputDir = $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\Log\";
        
        if(!Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
        
        var outputPath =
            $"{outputDir}{fileId}_g.txt";
        File.AppendAllText(outputPath, source.ToString());
    }

    public static void CreateLog(string source, string fileId)
    {
        var outputDir = $@"E:\repos\serialize-interface-generator\Unity_SerializeInterfaceGenerator\Assets\SerializeInterface\Log\";
        
        if(!Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
        
        var outputPath =
            $"{outputDir}{fileId}_g.txt";
        File.WriteAllText(outputPath, source.ToString());
        
        
    }

    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            // Identify classes that contain fields marked with the [SerializeInterface] attribute
            if (node is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .Any(f => f.AttributeLists.Count > 0))
            {
                Classes.Add(classDeclaration);
            }
        }
    }
}