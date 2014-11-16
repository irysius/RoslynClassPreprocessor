using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RoslynClassPreprocessor
{
	public class FilterConfigRoslynProcessor
	{
		public FilterConfigRoslynProcessor() { }

		public SyntaxNode LocateFilterAttribute(SyntaxNode root, string attributeName) {
			var @class = FindClass(root);
			if (@class == null) return null;
			var method = FindMethod(@class);
			if (method == null) return null;
			return FindStatement(method, attributeName);
		}

		public SyntaxNode RemoveFilterAttribute(SyntaxNode root, string attributeName) {
			var filterAttribute = LocateFilterAttribute(root, attributeName);
			if (filterAttribute == null) return root;
			return root.RemoveNode(filterAttribute, SyntaxRemoveOptions.KeepNoTrivia);
		}

		public SyntaxNode AddFilterAttribute(SyntaxNode root, string name, string @using) {
			if (!RoslynHelpers.UsingDirectives.HasUsing(root, @using)) {
				root = RoslynHelpers.UsingDirectives.AddUsing(root, @using);
			}

			var @class = FindClass(root);
			if (@class == null) {
				var @namespace = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
				if (!@namespace.DescendantNodes().Where(n => !(n is IdentifierNameSyntax)).Any()) {
					// Should the namespace be empty, replace the entire namespace.
					var namespaceIdentifier = @namespace.DescendantNodes().OfType<IdentifierNameSyntax>()
						.First().Identifier.ToString();
					var newNode = CSharpSyntaxTree.ParseText(GenerateNamespace(name, namespaceIdentifier));
					root = root.ReplaceNode(@namespace, newNode.GetRoot().DescendantNodes().First());
					return root;
				} else {
					// Should the namespace not be empty (why is there another class in FilterConfig anyways?),
					// insert after the last class or whatever declaration.
					var lastNode = @namespace.ChildNodes().Last();
					var newTree = CSharpSyntaxTree.ParseText(GenerateClass(name));
					var newNode = newTree.GetRoot().DescendantNodes().First();
					newNode = newNode.WithLeadingTrivia(RoslynHelpers.LeadingNewLine);

					return root.InsertNodesAfter(lastNode, new[] { newNode });
				}
			}
			var method = FindMethod(@class);
			if (method == null) {
				if (!@class.DescendantNodes().Any()) {
					// Should the class be empty, replace the entire class.
					// Note that the root itself is a CompilationUnit, so the replacement is the child.
					var newNode = CSharpSyntaxTree.ParseText(GenerateClass(name));
					root = root.ReplaceNode(@class, newNode.GetRoot().ChildNodes().First());
					return root;
				} else {
					// Should the class not be empty, insert after the last "statement".
					var lastNode = @class.ChildNodes().Last();
					var newNode = CSharpSyntaxTree.ParseText(GenerateMethod(name));
					
					return root.InsertNodesAfter(lastNode, new []{ newNode.GetRoot().DescendantNodes().First() });
				}
			}

			var addFilterStatement = SyntaxFactory.ParseStatement(GenerateStatement(name));
			var methodBlock = method.DescendantNodes().OfType<BlockSyntax>().First();
			var newMethodBlock = methodBlock.AddStatements(addFilterStatement);
			return root.ReplaceNode(methodBlock, newMethodBlock);
		}

		private ClassDeclarationSyntax FindClass(SyntaxNode root) {
			return root.DescendantNodes().OfType<ClassDeclarationSyntax>()
				.FirstOrDefault(n => n.Identifier.Text == "FilterConfig");
		}

		private MethodDeclarationSyntax FindMethod(ClassDeclarationSyntax @class) {
			return @class.DescendantNodes().OfType<MethodDeclarationSyntax>()
				.FirstOrDefault(n => n.Identifier.Text == "RegisterGlobalFilters");
		}

		private ExpressionStatementSyntax FindStatement(MethodDeclarationSyntax method, string attributeName) {
			// Search for statement with method call .Add
			// Search for parameter with new()
			var statements = method.DescendantNodes().OfType<ExpressionStatementSyntax>();
			foreach (var statement in statements) {
				bool isAddCalled = statement.DescendantNodes().OfType<InvocationExpressionSyntax>()
					.SelectMany(n => n.DescendantNodes().OfType<IdentifierNameSyntax>())
					.Any(n => n.Identifier.ToString() == "Add");
				if (isAddCalled) {
					var objectCreationCallNames = statement.DescendantNodes().OfType<ArgumentSyntax>()
						.SelectMany(n => n.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
						.SelectMany(n => n.DescendantNodes().OfType<IdentifierNameSyntax>());
					if (objectCreationCallNames.Any(n => n.Identifier.ToString() == attributeName)) {
						return statement;
					}
				}
			}
			return null;
		}

		private string GenerateStatement(string attributeName) {
			return String.Format("filters.Add(new {0}());", attributeName.Trim());
		}
		private string GenerateMethod(string attributeName) {
			return @"public static void RegisterGlobalFilters(GlobalFilterCollection filters)
{
	" + GenerateStatement(attributeName) + @"		
}";
		}
		public string GenerateClass(string attributeName) {
			return @"public class FilterConfig
{
	" + GenerateMethod(attributeName) + @"   
}";
		}
		private string GenerateNamespace(string attributeName, string @namespace) {
			return "namespace " + @namespace + @"{
	" + GenerateClass(attributeName) + @"
}";
		}
	}
}
