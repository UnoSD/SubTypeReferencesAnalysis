using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace SubTypeReferencesAnalysis
{
    public class FindAllClassesSpeedTest
    {
        readonly Compilation _compilation;
        readonly ClassesVisitor _classVisitor;

        public FindAllClassesSpeedTest(string solutionPath)
        {
            var workspace = MSBuildWorkspace.Create();
            var project = workspace.OpenProjectAsync(solutionPath).Result;

            _compilation = project.GetCompilationAsync().Result;

            _classVisitor = new ClassesVisitor();
        }

        public void Test()
        {
            Measure(FindTypes1, nameof(FindTypes1)); // Fast and cached.
            Measure(FindTypes1, nameof(FindTypes1)); // Fast and cached.
            Measure(FindTypes2, nameof(FindTypes2)); // Fast and cached.
            Measure(FindTypes2, nameof(FindTypes2)); // Fast and cached.
            Measure(FindTypes3, nameof(FindTypes3)); // Slowest.
            Measure(FindTypes3, nameof(FindTypes3)); // Slowest.
        }

        static void Measure(Func<IReadOnlyCollection<string>> execute, string title)
        {
            Console.WriteLine(title);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            var findTypes = execute();

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed.Ticks;

            Console.WriteLine($"    {elapsed}");
            Console.WriteLine($"    {findTypes.Count}");
            findTypes.Select(name => $"    {name}").ToList().ForEach(Console.WriteLine);
        }

        IReadOnlyCollection<string> FindTypes1()
        {
            return _compilation.SyntaxTrees.AsParallel().SelectMany
                (
                    tree => tree.GetRoot()
                        .DescendantNodesAndSelf()
                        .AsParallel()
                        .OfType<ClassDeclarationSyntax>()
                )
                .Select(syntax => syntax.Identifier.ValueText).ToList();
        }

        IReadOnlyCollection<string> FindTypes2()
        {
            return _compilation.SyntaxTrees.AsParallel().SelectMany
                (
                    tree => tree.GetRoot()
                        .DescendantNodesAndSelf()
                        .AsParallel()
                        .Where(node => node.IsKind(SyntaxKind.ClassDeclaration))
                        .OfType<ClassDeclarationSyntax>()
                )
                .Select(syntax => syntax.Identifier.ValueText).ToList();
        }
        IReadOnlyCollection<string> FindTypes3()
        {
            // Will get exception when accessing to the non-concurrent list.
            return _compilation.SyntaxTrees.AsParallel().SelectMany(tree =>
            {
                _classVisitor.Classes.Clear();

                _classVisitor.Visit(tree.GetRoot());

                return _classVisitor.Classes;
            }).ToList();
        }
    }
}