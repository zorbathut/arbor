namespace Arbor
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Collections.Generic;
    using System.Linq;

    [Generator]
    internal class ParamGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var arborNode = context.Compilation.GetTypeByMetadataName("Arbor.Node");
            var arborBlackboardParameter = context.Compilation.GetTypeByMetadataName("Arbor.BlackboardParameter`1");

            var fullyQualified = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

            var nowhereLocation = Location.Create(context.Compilation.SyntaxTrees.First(), new Microsoft.CodeAnalysis.Text.TextSpan(1, 2));

            if (arborNode == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("a", "", "missin' arbor node", "", DiagnosticSeverity.Error, true), nowhereLocation));
            }
            if (arborBlackboardParameter == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("a", "", "missin' arbor bbp", "", DiagnosticSeverity.Error, true), nowhereLocation));
            }

            foreach (var type in context.Compilation.SyntaxTrees.SelectMany(tree =>
                tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Select (cds => context.Compilation.GetSemanticModel(tree).GetDeclaredSymbol(cds)).Where(type =>
                {
                    System.Diagnostics.Debug.WriteLine($"Processing {type}");
                    while (type != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(type, arborNode))
                        {
                            System.Diagnostics.Debug.WriteLine($"yay");
                            return true;
                        }

                        type = type.BaseType;
                    }
                        
                    return false;
                })
            ))
            {
                System.Diagnostics.Debug.WriteLine($"  really doing {type}");

                var nodeNamespace = type.ContainingNamespace?.ToDisplayString(fullyQualified);

                bool foundSomething = false;

                var source = new System.Text.StringBuilder();
                if (nodeNamespace != null)
                {
                    source.AppendLine($"namespace {nodeNamespace} {{");
                }

                // nested class time
                var typeNesting = new List<INamedTypeSymbol>();
                {
                    INamedTypeSymbol currentType = type;
                    while (currentType != null)
                    {
                        typeNesting.Add(currentType);
                        currentType = currentType.ContainingType;
                    }
                }

                foreach (var typeHierarchy in Enumerable.Reverse(typeNesting))
                {
                    source.AppendLine($"partial class {typeHierarchy.Name} {{");
                }

                var register = new System.Text.StringBuilder();
                foreach (var bbp in type.GetMembers().OfType<IFieldSymbol>())
                {
                    if (!(bbp.Type is INamedTypeSymbol namedType))
                    {
                        continue;
                    }

                    if (!SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, arborBlackboardParameter))
                    {
                        continue;
                    }

                    // yay we have a match

                    if (!bbp.Name.EndsWith("Id"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("a", "", "no bad id suffix", "", DiagnosticSeverity.Error, true), nowhereLocation));
                    }

                    foundSomething = true;

                    var typeString = namedType.TypeArguments[0].ToDisplayString(fullyQualified);
                    source.AppendLine($"{typeString} {bbp.Name.RemoveSuffix("Id")} {{");
                    source.AppendLine($"  get => {bbp.Name}.Get(Tree);");
                    source.AppendLine($"  set => {bbp.Name}.Set(Tree, value);");
                    source.AppendLine($"}}");

                    register.AppendLine($"{bbp.Name}.Register(Tree);");
                }

                source.AppendLine($"public override void Register() {{");
                source.AppendLine($"  base.Register();");
                source.AppendLine(register.ToString());
                source.AppendLine($"}}");

                foreach (var typeHierarchy in typeNesting)
                {
                    source.AppendLine($"}}");
                }

                if (nodeNamespace != null)
                {
                    source.AppendLine($"}}");
                }

                if (foundSomething)
                {
                    context.AddSource($"{type.Name}.g.cs", source.ToString());
                }
            }
        }

        public IEnumerable<INamedTypeSymbol> FindInheritedFrom(INamespaceSymbol context, INamedTypeSymbol parent)
        {
            foreach (var type in context.GetTypeMembers().Where(type =>
                {
                    if (type.TypeKind != TypeKind.Class)
                    {
                        return false;
                    }

                    while (type != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(type, parent))
                        {
                            return true;
                        }

                        type = type.BaseType;
                    }

                    return false;
                }))
            {
                yield return type;
            }

            foreach (var ns in context.GetNamespaceMembers())
            {
                foreach (var type in FindInheritedFrom(ns, parent))
                {
                    yield return type;
                }
            }
        }
    }
}
