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
	public class RoslynNodeReplacer: CSharpSyntaxRewriter
	{
		FilterConfigRoslynProcessor processor = new FilterConfigRoslynProcessor();
		public override SyntaxNode Visit(SyntaxNode node)
		{
			if (node is ClassDeclarationSyntax) {
				var casted = node as ClassDeclarationSyntax;
				if (casted.Identifier.Text == "FilterConfig") {
					var testTree = SyntaxFactory.ParseSyntaxTree(processor.GenerateClass("CustomFilter")).GetRoot();
					return testTree;
				}
			}
			return base.Visit(node);
		}
	}
}
