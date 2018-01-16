﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Build.Core.RazorCompilation;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.Rendering;
using Microsoft.Blazor.RenderTree;
using Microsoft.Blazor.Test.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Microsoft.Blazor.Build.Test
{
    public class RazorCompilerTest
    {
        [Fact]
        public void RejectsInvalidClassName()
        {
            // Arrange/Act
            var result = CompileToCSharp(
                "x:\\dir\\subdir",
                "Filename with spaces.cshtml",
                "ignored code",
                "ignored namespace");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal(RazorCompilerDiagnostic.DiagnosticType.Error, item.Type);
                    Assert.StartsWith($"Invalid name 'Filename with spaces'", item.Message);
                });
        }

        [Theory]
        [InlineData("\\unrelated.cs")]
        [InlineData("..\\outsideroot.cs")]
        public void RejectsFilenameOutsideRoot(string filename)
        {
            // Arrange/Act
            var result = CompileToCSharp(
                "x:\\dir\\subdir",
                filename,
                "ignored code",
                "ignored namespace");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal(RazorCompilerDiagnostic.DiagnosticType.Error, item.Type);
                    Assert.StartsWith($"File is not within source root directory: '{filename}'", item.Message);
                });
        }

        [Theory]
        [InlineData("ItemAtRoot.cs", "Test.Base", "ItemAtRoot")]
        [InlineData(".\\ItemAtRoot.cs", "Test.Base", "ItemAtRoot")]
        [InlineData("x:\\dir\\subdir\\ItemAtRoot.cs", "Test.Base", "ItemAtRoot")]
        [InlineData("Dir1\\MyFile.cs", "Test.Base.Dir1", "MyFile")]
        [InlineData("Dir1\\Dir2\\MyFile.cs", "Test.Base.Dir1.Dir2", "MyFile")]
        public void CreatesClassWithCorrectNameAndNamespace(string relativePath, string expectedNamespace, string expectedClassName)
        {
            // Arrange/Act
            var result = CompileToAssembly(
                "x:\\dir\\subdir",
                relativePath,
                "{* No code *}",
                "Test.Base");

            // Assert
            Assert.Empty(result.Diagnostics);
            Assert.Collection(result.Assembly.GetTypes(),
                type =>
                {
                    Assert.Equal(expectedNamespace, type.Namespace);
                    Assert.Equal(expectedClassName, type.Name);
                });
        }

        [Fact]
        public void CanRenderPlainText()
        {
            // Arrange
            var treeBuilder = new RenderTreeBuilder(new TestRenderer());

            // Arrange/Act
            var component = CompileToComponent("Some plain text");
            component.BuildRenderTree(treeBuilder);

            // Assert
            Assert.Collection(treeBuilder.GetNodes(),
                node => AssertNode.Text(node, "Some plain text"));
        }

        [Fact]
        public void CanUseCSharpFunctionsBlock()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
                @foreach(var item in items) {
                    @item
                }
                @functions {
                    string[] items = new[] { ""First"", ""Second"", ""Third"" };
                }
            ");

            // Assert
            var nodes = GetRenderTree(component).Where(NotWhitespace);
            Assert.Collection(nodes,
                node => AssertNode.Text(node, "First"),
                node => AssertNode.Text(node, "Second"),
                node => AssertNode.Text(node, "Third"));
        }

        [Fact]
        public void CanRenderElements()
        {
            // Arrange/Act
            var component = CompileToComponent("<myelem>Hello</myelem>");

            // Assert
            var nodes = GetRenderTree(component).Where(NotWhitespace);
            Assert.Collection(nodes,
                node => AssertNode.Element(node, "myelem", 1),
                node => AssertNode.Text(node, "Hello"));
        }

        private static bool NotWhitespace(RenderTreeNode node)
            => node.NodeType != RenderTreeNodeType.Text
            || !string.IsNullOrWhiteSpace(node.TextContent);

        private static ArraySegment<RenderTreeNode> GetRenderTree(IComponent component)
        {
            var treeBuilder = new RenderTreeBuilder(new TestRenderer());
            component.BuildRenderTree(treeBuilder);
            return treeBuilder.GetNodes();
        }

        private static IComponent CompileToComponent(string cshtmlSource)
        {
            var testComponentTypeName = "TestComponent";
            var testComponentNamespace = "Test";
            var assemblyResult = CompileToAssembly("c:\\ignored", $"{testComponentTypeName}.cshtml", cshtmlSource, testComponentNamespace);
            Assert.Empty(assemblyResult.Diagnostics);
            var testComponentType = assemblyResult.Assembly.GetType($"{testComponentNamespace}.{testComponentTypeName}");
            return (IComponent)Activator.CreateInstance(testComponentType);
        }

        private static CompileToAssemblyResult CompileToAssembly(string cshtmlRootPath, string cshtmlRelativePath, string cshtmlContent, string outputNamespace)
        {
            var csharpResult = CompileToCSharp(cshtmlRootPath, cshtmlRelativePath, cshtmlContent, outputNamespace);
            if (csharpResult.Diagnostics.Any())
            {
                var diagnosticsLog = string.Join(Environment.NewLine,
                    csharpResult.Diagnostics.Select(d => d.FormatForConsole()).ToArray());
                throw new InvalidOperationException($"Aborting compilation to assembly because RazorCompiler returned nonempty diagnostics: {diagnosticsLog}");
            }

            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(csharpResult.Code)
            };
            var referenceAssembliesContainingTypes = new[]
            {
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute), // System.Runtime
                typeof(BlazorComponent)
            };
            var references = referenceAssembliesContainingTypes
                .SelectMany(type => type.Assembly.GetReferencedAssemblies().Concat(new[] { type.Assembly.GetName() }))
                .Distinct()
                .Select(Assembly.Load)
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                .ToList();
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var assemblyName = "TestAssembly" + Guid.NewGuid().ToString("N");
            var compilation = CSharpCompilation.Create(assemblyName,
                syntaxTrees,
                references,
                options);

            using (var peStream = new MemoryStream())
            {
                compilation.Emit(peStream);

                var diagnostics = compilation.GetDiagnostics();
                return new CompileToAssemblyResult
                {
                    Diagnostics = diagnostics,
                    VerboseLog = csharpResult.VerboseLog,
                    Assembly = diagnostics.Any() ? null : Assembly.Load(peStream.ToArray())
                };
            }
        }

        private static CompileToCSharpResult CompileToCSharp(string cshtmlRootPath, string cshtmlRelativePath, string cshtmlContent, string outputNamespace)
        {
            using (var resultStream = new MemoryStream())
            using (var resultWriter = new StreamWriter(resultStream))
            using (var verboseLogStream = new MemoryStream())
            using (var verboseWriter = new StreamWriter(verboseLogStream))
            using (var inputContents = new MemoryStream(Encoding.UTF8.GetBytes(cshtmlContent)))
            {
                var diagnostics = new RazorCompiler().CompileSingleFile(
                    cshtmlRootPath,
                    cshtmlRelativePath,
                    inputContents,
                    outputNamespace,
                    resultWriter,
                    verboseWriter);

                resultWriter.Flush();
                verboseWriter.Flush();
                return new CompileToCSharpResult
                {
                    Code = Encoding.UTF8.GetString(resultStream.ToArray()),
                    VerboseLog = Encoding.UTF8.GetString(verboseLogStream.ToArray()),
                    Diagnostics = diagnostics
                };
            }
        }

        private class CompileToCSharpResult
        {
            public string Code { get; set; }
            public string VerboseLog { get; set; }
            public IEnumerable<RazorCompilerDiagnostic> Diagnostics { get; set; }
        }

        private class CompileToAssemblyResult
        {
            public Assembly Assembly { get; set; }
            public string VerboseLog { get; set; }
            public IEnumerable<Diagnostic> Diagnostics { get; set; }
        }

        private class TestRenderer : Renderer
        {
            protected override void UpdateDisplay(int componentId, ArraySegment<RenderTreeNode> renderTree)
                => throw new NotImplementedException();
        }
    }
}