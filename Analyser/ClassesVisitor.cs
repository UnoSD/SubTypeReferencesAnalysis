using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SubTypeReferencesAnalysis
{
    class ClassesVisitor : CSharpSyntaxRewriter
    {
        public List<string> Classes { get; } = new List<string>();

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax syntaxNode)
        {
            syntaxNode = base.VisitClassDeclaration(syntaxNode) as ClassDeclarationSyntax;

            var className = syntaxNode?.Identifier.ValueText;

            this.Classes.Add(className);

            return syntaxNode;
        }
    }
}