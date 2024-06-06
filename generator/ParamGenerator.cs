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
            var arborNodeType = context.Compilation.GetTypeByMetadataName("Arbor.Node");
            var arborBlackboardParameterType = context.Compilation.GetTypeByMetadataName("Arbor.BlackboardParameter`1");
            var listType = context.Compilation.GetTypeByMetadataName(typeof(List<>).FullName);
            var arrayType = context.Compilation.GetTypeByMetadataName(typeof(System.Array).FullName);

            var fullyQualified = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

            var nowhereLocation = Location.Create(context.Compilation.SyntaxTrees.First(), new Microsoft.CodeAnalysis.Text.TextSpan(1, 2));

            if (arborNodeType == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("a", "", "missin' arbor node", "", DiagnosticSeverity.Error, true), nowhereLocation));
            }
            if (arborBlackboardParameterType == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("a", "", "missin' arbor bbp", "", DiagnosticSeverity.Error, true), nowhereLocation));
            }

            foreach (var type in context.Compilation.SyntaxTrees.SelectMany(tree =>
                tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Select(cds => context.Compilation.GetSemanticModel(tree).GetDeclaredSymbol(cds)).Where(type => type.InheritsFrom(arborNodeType))))
            {
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

                var initFields = new System.Text.StringBuilder();
                var resetFields = new System.Text.StringBuilder();
                foreach (var bbp in type.GetMembers().OfType<IFieldSymbol>())
                {
                    if (!(bbp.Type is INamedTypeSymbol namedType))
                    {
                        continue;
                    }

                    // Check to see if this is a blackboard parameter
                    if (SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, arborBlackboardParameterType))
                    {
                        if (!bbp.Name.EndsWith("Id"))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("a", "", "Blackboard parameters must have an `Id` suffix.", "", DiagnosticSeverity.Error, true), nowhereLocation));
                        }

                        foundSomething = true;

                        var typeString = namedType.TypeArguments[0].ToDisplayString(fullyQualified);
                        source.AppendLine($"protected {typeString} {bbp.Name.RemoveSuffix("Id")} {{");
                        source.AppendLine($"  get => {bbp.Name}.Get();");
                        source.AppendLine($"  set => {bbp.Name}.Set(value);");
                        source.AppendLine($"}}");

                        initFields.AppendLine($"{bbp.Name}.Register();");
                    }

                    // Check to see if this is derived from a Node
                    if (namedType.InheritsFrom(arborNodeType))
                    {
                        foundSomething = true;
                        initFields.AppendLine($"{bbp.Name}.Init();");
                        resetFields.AppendLine($"{bbp.Name}.Reset();");
                    }


                    var genericType = namedType.ConstructedFrom;
                    if (
                        SymbolEqualityComparer.Default.Equals(genericType.ConstructedFrom, listType) ||    // Check to see if this is a List<Node> or similar
                        SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, arrayType)       // Check to see if this is a Node[] or similar
                        )
                    {
                        var typeArgument = namedType.TypeArguments.FirstOrDefault();
                        if (typeArgument.InheritsFrom(arborNodeType))
                        {
                            foundSomething = true;
                            initFields.AppendLine($"foreach (var item in {bbp.Name}) item?.Init();");
                            resetFields.AppendLine($"foreach (var item in {bbp.Name}) item?.Reset();");
                        }
                    }
                }

                source.AppendLine($"public override void InitFields() {{");
                source.AppendLine($"  base.InitFields();");
                source.AppendLine(initFields.ToString());
                source.AppendLine($"}}");

                source.AppendLine($"public override void ResetFields() {{");
                source.AppendLine($"  base.ResetFields();");
                source.AppendLine(resetFields.ToString());
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
