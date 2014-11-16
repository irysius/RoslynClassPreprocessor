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
	public static class RoslynHelpers
	{
		static RoslynHelpers() {
			LeadingSpace = SyntaxFactory.ParseLeadingTrivia(" ");
			TrailingNewLine = SyntaxFactory.ParseTrailingTrivia("\r\n");
			LeadingNewLine = SyntaxFactory.ParseLeadingTrivia("\r\n");
		}

		public static SyntaxTriviaList LeadingSpace;
		public static SyntaxTriviaList TrailingNewLine;
		public static SyntaxTriviaList LeadingNewLine;
		public static class UsingDirectives {
			public static bool HasUsing(SyntaxNode root, string name) {
				return root.DescendantNodes().OfType<UsingDirectiveSyntax>()
					.Any(n => n.Name.ToString() == name);
			}
			public static UsingDirectiveSyntax CreateUsing(string name) {
				var usingName = SyntaxFactory.ParseName(name);
				usingName = usingName.WithLeadingTrivia(LeadingSpace);
				var usingDirective = SyntaxFactory.UsingDirective(usingName);
				usingDirective = usingDirective.WithTrailingTrivia(TrailingNewLine);
				return usingDirective;
			}
			public static SyntaxNode AddUsing(SyntaxNode root, string name) {
				var lastUsing = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
					.LastOrDefault();
				if (lastUsing == null) {
					var firstNode = root.DescendantNodes().First();
					return root.InsertNodesBefore(firstNode, new[] { CreateUsing(name) });
				} else {
					return root.InsertNodesAfter(lastUsing, new[] { CreateUsing(name) });
				}
			}
			public static SyntaxNode RemoveUsing(SyntaxNode root, string name) {
				var @using = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
					.FirstOrDefault(n => n.Name.ToString() == name);

				if (@using != null) {
					return root.RemoveNode(@using, SyntaxRemoveOptions.KeepNoTrivia);
				}
				return root;
			}
		}
	}
}
