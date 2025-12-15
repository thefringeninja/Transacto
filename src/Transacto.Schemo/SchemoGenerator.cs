using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;

namespace Transacto.Schemo;

[Generator]
#pragma warning disable RS1036
public class SchemoGenerator : IIncrementalGenerator {
#pragma warning restore RS1036
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var rootNamespaceProvider = context.AnalyzerConfigOptionsProvider
			.Select((provider, _) => {
				provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
				return rootNamespace;
			});


		var schemata = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".schema.json"));
		var items = schemata
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Combine(rootNamespaceProvider)
			.Select((item, ct) => {
				var ((text, optionsProvider), rootNamespace) = item;

				var options = optionsProvider.GetOptions(text);
				options.TryGetValue("build_metadata.AdditionalFiles.Namespace", out var fileNamespace);

				var effectiveNamespace = !string.IsNullOrWhiteSpace(fileNamespace)
					? fileNamespace
					: rootNamespace;

				return (name: Path.GetFileNameWithoutExtension(text.Path), path: text.Path,
					@namespace: effectiveNamespace,
					source: text.GetText(ct)!.ToString());
			});

		context.RegisterSourceOutput(items, (spc, item) => {
			var schema = JsonSchema.FromFileAsync(item.path, spc.CancellationToken).Result;
			var generator = new CSharpGenerator(schema, new() {
				Namespace = item.@namespace,
				ClassStyle = CSharpClassStyle.Record,
				SchemaType = SchemaType.JsonSchema,
				JsonLibrary = CSharpJsonLibrary.SystemTextJson,
				ArrayType = "System.Collections.Immutable.ImmutableArray",
				ArrayInstanceType = "System.Collections.Immutable.ImmutableArray",
				DictionaryType = "System.Collections.Immutable.ImmutableSortedDictionary",
				DictionaryInstanceType = "System.Collections.Immutable.ImmutableSortedDictionary",
				GenerateDataAnnotations = false,
				UseRequiredKeyword = true,
				SortConstructorParameters = false,
				GenerateDefaultValues = false,
				PropertySetterAccessModifier = "init",
				GenerateNativeRecords = true,
				TypeNameGenerator = new TypeNameGenerator()
			});
			var syntaxTree = CSharpSyntaxTree.ParseText(generator.GenerateFile());

			var rewriter = new RewriteClassesToRecords();

			var result = rewriter.Visit(syntaxTree.GetCompilationUnitRoot());

			spc.AddSource(item.name, result.NormalizeWhitespace().GetText(Encoding.UTF8));
		});
	}

	private class TypeNameGenerator : ITypeNameGenerator {
		public string Generate(JsonSchema schema, string typeNameHint, IEnumerable<string> reservedTypeNames) {
			return schema.Title?.Replace(" ", string.Empty)!;
		}
	}

	private class RewriteClassesToRecords : CSharpSyntaxRewriter {
		public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node) =>
			SyntaxFactory.RecordDeclaration(
					node.AttributeLists,
					node.Modifiers,
					SyntaxFactory.Token(SyntaxKind.RecordKeyword),
					node.Identifier,
					node.TypeParameterList, null, node.BaseList, node.ConstraintClauses,
					SyntaxFactory.Token(SyntaxKind.OpenBraceToken), default,
					SyntaxFactory.Token(SyntaxKind.CloseBraceToken), node.SemicolonToken)
				.AddMembers(node.Members.OfType<PropertyDeclarationSyntax>().Select(BuildProperty).Where(member =>
					member switch {
						PropertyDeclarationSyntax p => p.Identifier.Text != "AdditionalProperties",
						_ => true
					}).ToArray())
				.NormalizeWhitespace();

		private static PropertyDeclarationSyntax BuildProperty(PropertyDeclarationSyntax prop) {
			return SyntaxFactory
				.PropertyDeclaration(prop.Type, prop.Identifier)
				.AddModifiers(prop.Modifiers.ToArray())
				.AddAttributeLists(prop.AttributeLists.ToArray())
				.AddAccessorListAccessors(
					SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
						.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
					SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
						.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
		}
	}
}
