using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynClassPreprocessor
{
	class Program
	{
		static bool isRunning = true;
		static string sourceFolder;
		static string targetFolder;
		static void Main(string[] args)
		{
			sourceFolder = Path.Combine(Environment.CurrentDirectory, "../../Input");
			targetFolder = Path.Combine(Environment.CurrentDirectory, "../../Output");
			while (isRunning) {
				var command = Console.ReadLine();
				ProcessCommand(command);
			}
		}

		static void ProcessCommand(string command) {
			command = command.Trim().ToLower();

			switch (command) { 
				case "processfilterconfig":
					Console.WriteLine("Provide attributes to remove (eg., AttributeOne, AttributeTwo):");
					var attributesToRemove = Console.ReadLine().Split(',')
						.Where(s => !String.IsNullOrWhiteSpace(s))
						.Select(s => s.Trim())
						.Distinct()
						.ToList();
					Console.WriteLine("Provide attributes to include, with namespace");
					Console.WriteLine("(eg., AttributeOne|LibraryOne.Filters, AttributeTwo|LibraryTwo.Tools):");
					var attributesToIncludeWithNamespace = Console.ReadLine().Split(',')
						.Select(s =>
						{
							var parts = s.Split('|');
							if (parts.Length != 2) return null;
							return Tuple.Create(parts[0].Trim(), parts[1].Trim());
						})
						.Where(s =>
						{
							if (s == null) return false;
							if (String.IsNullOrWhiteSpace(s.Item1)) return false;
							if (String.IsNullOrWhiteSpace(s.Item2)) return false;
							return true;
						})
						.ToList();
						
					ProcessFilterConfig(attributesToRemove, attributesToIncludeWithNamespace);
					Console.WriteLine("FilterConfig.cs processed.");
					Console.WriteLine("");
					break;
				case "processglobal":
					Console.WriteLine("Not Yet Implemented");
					Console.WriteLine("");
					break;
				case "exit":
					Console.WriteLine("Exiting console.");
					isRunning = false;
					break;
				case "clear":
					Console.Clear();
					break;
				case "help":
				default:
					DisplayHelp();
					break;
			}
		}

		static void ProcessFilterConfig(List<string> removeAttributes, List<Tuple<string, string>> insertAttributesWithNamespaces) {
			string sourceFile = Path.Combine(sourceFolder, "FilterConfig.cs");
			var sourceText = File.ReadAllText(sourceFile);
			var sourceTree = CSharpSyntaxTree.ParseText(sourceText);
			var sourceRoot = sourceTree.GetRoot();

			var children = sourceRoot.ChildNodes().ToList();

			var filterConfigProcessor = new FilterConfigRoslynProcessor();

			foreach (var attribute in removeAttributes) {
				sourceRoot = filterConfigProcessor.RemoveFilterAttribute(sourceRoot, attribute);
			}
			foreach (var pair in insertAttributesWithNamespaces) {
				sourceRoot = filterConfigProcessor.AddFilterAttribute(sourceRoot, pair.Item1, pair.Item2);
			}

			string newFile = Path.Combine(targetFolder, "FilterConfig.cs");
			Workspace workspace = new CustomWorkspace();
			var options = workspace.Options
				.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, false);

			sourceRoot = Formatter.Format(sourceRoot, workspace, options);
			using (TextWriter writer = File.CreateText(newFile)) {
				sourceRoot.WriteTo(writer);
			}
			
		}

		static void DisplayHelp() {
			Console.WriteLine("processfilterconfig");
			Console.WriteLine("  - processes FilterConfig.cs");
			Console.WriteLine("processglobal");
			Console.WriteLine("  - processes Global.asax.cs");
			Console.WriteLine("clear");
			Console.WriteLine("  - clears the console of text.");
			Console.WriteLine("exit");
			Console.WriteLine("  - exits the console.");
			Console.WriteLine("help");
			Console.WriteLine("  - displays this help text.");
		}
	}
}
