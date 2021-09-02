using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

namespace Transacto {
	[Generator]
	public class SchemoGenerator : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) {
		}

		public void Execute(GeneratorExecutionContext context) {
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace",
				out var rootNamespace);

			var cts = new CancellationTokenSource();

			var rewriter = new RewriteClassesToRecords();

			foreach (var schema in GenerateSchemas()) {
				var syntaxTree = CSharpSyntaxTree.ParseText(schema.Source);

				var result = rewriter.Visit(syntaxTree.GetCompilationUnitRoot());

				context.AddSource(schema.Name, result.NormalizeWhitespace().GetText(Encoding.UTF8));
			}

			GeneratedSchema[] GenerateSchemas() {
				var fileTasks = (
					from file in context.AdditionalFiles
					where file.Path.EndsWith(".schema.json")
					select GenerateSchema(file.Path, GetNamespace(file), cts.Token)).ToArray();

				Task.WaitAll(fileTasks);
				return Array.ConvertAll(fileTasks, fileTask => fileTask.Result);
			}

			string GetNamespace(AdditionalText file) =>
				context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.Namespace",
					out var @namespace)
				&& !string.IsNullOrWhiteSpace(@namespace)
					? @namespace
					: rootNamespace!;
		}

		private static async Task<GeneratedSchema> GenerateSchema(string path, string @namespace,
			CancellationToken cancellationToken) {
			var name = Path.GetFileNameWithoutExtension(path).Split('.')[0];

			var generator = new CSharpGenerator(await JsonSchema.FromFileAsync(path, cancellationToken), new() {
				Namespace = @namespace,
				ClassStyle = CSharpClassStyle.Poco,
				SchemaType = SchemaType.JsonSchema,
				JsonLibrary = CSharpJsonLibrary.SystemTextJson,
				ArrayType = "System.Collections.Immutable.ImmutableArray",
				ArrayInstanceType = "System.Collections.Immutable.ImmutableArray"
			});

			return new(name, generator.GenerateFile(name));
		}

		private record GeneratedSchema(string Name, string Source);

		private class RewriteClassesToRecords : CSharpSyntaxRewriter {
			public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) =>
				SyntaxFactory.RecordDeclaration(
						node.AttributeLists,
						node.Modifiers,
						SyntaxFactory.Token(SyntaxKind.RecordKeyword),
						node.Identifier,
						node.TypeParameterList, default, node.BaseList, node.ConstraintClauses,
						SyntaxFactory.Token(SyntaxKind.OpenBraceToken), default,
						SyntaxFactory.Token(SyntaxKind.CloseBraceToken), node.SemicolonToken)
					.AddMembers(node.Members.Select(member => member switch {
						PropertyDeclarationSyntax prop => SyntaxFactory.PropertyDeclaration(
								prop.Type, prop.Identifier)
							.AddModifiers(prop.Modifiers.ToArray())
							.AddAttributeLists(prop.AttributeLists.ToArray())
							.AddAccessorListAccessors(
								SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
									.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
								SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
									.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))),
						_ => member
					}).ToArray())
					.NormalizeWhitespace();
		}
	}
}
