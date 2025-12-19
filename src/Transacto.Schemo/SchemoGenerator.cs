using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using NodaTime;

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

		var projectDirectoryProvider = context.AnalyzerConfigOptionsProvider
			.Select((options, _) => {
				options.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory);
				return projectDirectory!;
			});

		var schemata = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".schema.json"));

		//SpinWait.SpinUntil(() => Debugger.IsAttached);

		var items = schemata
			.Combine(rootNamespaceProvider)
			.Combine(projectDirectoryProvider)
			.Select((item, ct) => {
				var ((text, rootNamespace), projectDirectory) = item;

				return (text, rootNamespace, projectDirectory);
			})
			.Select((item, ct) => {
				var (text, rootNamespace, projectDirectory) = item;

				var pieces = text.Path[(projectDirectory.Length + 1)..].Split('/', '\\').AsSpan();

				var f = new List<string>();
				if (!string.IsNullOrEmpty(rootNamespace)) {
					f.Add(rootNamespace!);
				}

				f.AddRange(pieces[..^1].ToArray());
				var @namespace = string.Join(".", f);
				var name = pieces[^1].Split('.')[0];
				var subpath = $"{@namespace}.{name}";
				var unit = (name, path: text.Path, subpath, @namespace, source: text.GetText(ct)!.ToString());

				return unit;
			}).Collect();

		var resolverFactory = (JsonSchema jsonSchema) => {
			return new JsonReferenceResolver(new JsonSchemaResolver(jsonSchema,
				new SystemTextJsonSchemaGeneratorSettings {
					TypeMappers = {
						new PrimitiveTypeMapper(typeof(LocalDate), s => {
							s.Type = JsonObjectType.String;
							s.Format = "date";
						}),
						new PrimitiveTypeMapper(typeof(OffsetDateTime), s => {
							s.Type = JsonObjectType.String;
							s.Format = "date-time";
						}),
					},
					TypeNameGenerator = new TypeNameGenerator()
				}));
		};

		context.RegisterSourceOutput(items, (spc, allItems) => {
			var rewriter = new RewriteClassesToRecords();

			foreach (var item in allItems) {
				spc.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						"SCHEMOO003",
						"Schema info",
						$"Generating schema. {(item.@namespace, item.name, item.path)}",
						nameof(SchemoGenerator),
						DiagnosticSeverity.Info,
						true),
					Location.None));

				var schema = JsonSchema.FromFileAsync(item.path,
					resolverFactory,
					spc.CancellationToken).Result;

				try {
					var generator = new CSharpGenerator(schema, new CodeGeneratorSettings {
						Namespace = item.@namespace
					});

					var sourceCode = new StringBuilder()
						.Append($"// {(item.@namespace, item.name, item.path)}")
						.Append(generator.GenerateFile())
						.ToString();

					var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

					var result = rewriter.Visit(syntaxTree.GetCompilationUnitRoot());

					if (result is null) {
						continue;
					}

					spc.AddSource(item.subpath, result.NormalizeWhitespace().GetText(Encoding.UTF8));
				} catch (Exception ex) {
					spc.ReportDiagnostic(Diagnostic.Create(
						new DiagnosticDescriptor(
							"SCHEMOO002",
							"Schema generation error",
							$"Failed to generate schema {item.name}: {ex.Message.Replace("\\r", "").Replace("\\n", "")}",
							nameof(SchemoGenerator),
							DiagnosticSeverity.Error,
							true),
						Location.None));
				}
			}
		});
	}

	private class TypeNameGenerator : ITypeNameGenerator {
		public string Generate(JsonSchema schema, string? typeNameHint, IEnumerable<string> reservedTypeNames) {
			return schema.Title?.Replace(" ", string.Empty)!;
		}
	}

	private class RewriteClassesToRecords : CSharpSyntaxRewriter {
		private readonly HashSet<string> _names = [];

		public override SyntaxNode? Visit(SyntaxNode? node) {
			if (node is not BaseTypeDeclarationSyntax typeNode) {
				return base.Visit(node);
			}

			Stack<string> s = [];

			s.Push(typeNode.Identifier.ToString());

			SyntaxNode current = typeNode;

			while (current.Parent is { } parent) {
				if (parent is TypeDeclarationSyntax t) {
					s.Push(t.Identifier.ToString());
					current = parent;
				} else if (parent is NamespaceDeclarationSyntax n) {
					s.Push(n.Name.ToString());
					current = parent;
				} else {
					break;
				}
			}

			var builder = new StringBuilder(s.Pop());
			while (s.Count > 0) {
				builder.Append('.').Append(s.Pop());
			}

			var name = builder.ToString();

			if (!_names.Add(name)) {
				return null;
			}

			return base.Visit(node);
		}

		public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node) {
			return SyntaxFactory.RecordDeclaration(
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
		}

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

	private class CodeGeneratorSettings : CSharpGeneratorSettings {
		public CodeGeneratorSettings() {
			ClassStyle = CSharpClassStyle.Record;
			SchemaType = SchemaType.OpenApi3;
			JsonLibrary = CSharpJsonLibrary.SystemTextJson;
			ArrayType = "System.Collections.Immutable.ImmutableArray";
			ArrayInstanceType = "System.Collections.Immutable.ImmutableArray";
			DictionaryType = "System.Collections.Immutable.ImmutableSortedDictionary";
			DictionaryInstanceType = "System.Collections.Immutable.ImmutableSortedDictionary";
			DateType = "NodaTime.LocalDate";
			DateTimeType = "NodaTime.OffsetDate";
			GenerateDataAnnotations = true;
			UseRequiredKeyword = true;
			SortConstructorParameters = false;
			GenerateDefaultValues = true;
			PropertySetterAccessModifier = "init";
			GenerateNativeRecords = true;
			TypeNameGenerator = new TypeNameGenerator();
		}
	}
}
