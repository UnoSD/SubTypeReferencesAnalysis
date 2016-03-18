using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace SubTypeReferencesAnalysis
{
    public class SubTypeReferencesFinder
    {
        readonly MSBuildWorkspace _workspace;
        readonly Compilation _compilation;
        readonly Project _project;

        readonly Func<string> _input;
        readonly Action<string> _output;
        
        public SubTypeReferencesFinder(string solutionPath, Action<string> output, Func<string> input)
        {
            _output = output;
            _input = input;

            _workspace = MSBuildWorkspace.Create();
            _project = _workspace.OpenProjectAsync(solutionPath).Result;
            _compilation = _project.GetCompilationAsync().Result;
        }

        public void StartSearch()
        {
            // Change all "Class" to "Type" and include structures.

            Func<ClassDeclarationSyntax, string> classNameSelector = syntax => syntax.Identifier.ValueText;
            Func<IMethodSymbol, string> methodNameSelector = symbol => symbol.Name;

            Action<IEnumerable<ClassDeclarationSyntax>> displayAllClasses = classes => classes.DisplayAll(classNameSelector);
            Action<IEnumerable<IMethodSymbol>> displayAllMethods = methods => methods.DisplayAll(methodNameSelector);

            Func<ClassDeclarationSyntax, string, bool> classSelector = (syntax, name) => SearchSelector(syntax, classNameSelector, name);
            Func<IMethodSymbol, string, bool> methodSelector = (syntax, name) => SearchSelector(syntax, methodNameSelector, name);

            var allClasses = GetClassDeclarations();

            displayAllClasses(allClasses);

            var searchClass = allClasses.IncreamentalSearch(classSelector, displayAllClasses, _input);

            _output($"*** Selected type: {searchClass?.Identifier.ValueText}");
            
            var allMethods = GetAllMethods(searchClass).ToArray();

            displayAllMethods(allMethods);

            var searchMethod = allMethods.IncreamentalSearch(methodSelector, displayAllMethods, _input);

            _output($"*** Selected method: {searchMethod?.Name}");

            Usages(searchMethod, searchClass?.SyntaxTree);
        }

        IEnumerable<IMethodSymbol> GetAllMethods(BaseTypeDeclarationSyntax searchClass)
        {
            var semanticModel = _compilation.GetSemanticModel(searchClass.SyntaxTree);

            var typeSymbol = semanticModel.GetDeclaredSymbol(searchClass);

            while (typeSymbol != null)
            {
                var methods = typeSymbol.GetMembers().OfType<IMethodSymbol>();

                foreach (var member in methods)
                    yield return member;

                typeSymbol = typeSymbol.BaseType;
            }
        }

        static bool SearchSelector<T>(T item, Func<T, string> getName, string name)
        {
            if (name.StartsWith("\"") && name.EndsWith("\""))
                return getName(item).Equals(name.Substring(1, name.Length - 2), StringComparison.InvariantCulture);

            return getName(item).ToLowerInvariant().Contains(name.ToLowerInvariant());
        }

        void Usages(IMethodSymbol method, SyntaxTree syntaxTree)
        {
            var semanticModel = _compilation.GetSemanticModel(syntaxTree);

            var receiverType = method.ReceiverType;

            var locations = SymbolFinder.FindReferencesAsync(method, _project.Solution).Result.SelectMany(symbol => symbol.Locations);

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var location in locations)
            {
                //var semanticModel = location.Document.GetSemanticModelAsync().Result;
                var invocation = SymbolFinder.FindSymbolAtPosition(semanticModel, location.Location.SourceSpan.Start, _workspace) as IMethodSymbol;

                if(invocation == null)
                    continue;

                if (invocation.ReceiverType.Equals(receiverType))
                    _output(GetDetails(location));
            }
        }

        IReadOnlyCollection<ClassDeclarationSyntax> GetClassDeclarations()
        {
            return _compilation.SyntaxTrees.AsParallel()
                                           .SelectMany(GetClassDeclarations)
                                           .ToArray();
        }

        static IEnumerable<ClassDeclarationSyntax> GetClassDeclarations(SyntaxTree tree)
        {
            return tree.GetRoot()
                       .DescendantNodesAndSelf()
                       .AsParallel()
                       .OfType<ClassDeclarationSyntax>();
        }

        static string GetDetails(ReferenceLocation location)
        {
            var textLineCollection = location.Document.GetTextAsync().Result.Lines;
            var lineSpan = location.Location.GetLineSpan();

            var line = lineSpan.StartLinePosition.Line;
            var character = lineSpan.StartLinePosition.Character;
            var filePath = lineSpan.Path;
            var preview = textLineCollection[line].ToString().Trim();

            return $@"{preview}{Environment.NewLine}{filePath}:{line + 1}:{character + 1}";
        }
    }
}
