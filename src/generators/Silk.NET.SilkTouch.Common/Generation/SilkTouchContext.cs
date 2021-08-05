﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Silk.NET.SilkTouch.Configuration;

namespace Silk.NET.SilkTouch.Generation
{
    public sealed record SilkTouchContext
    (
        string AssemblyName,
        IEnumerable<CSharpSyntaxTree> SyntaxTrees,
        ProjectConfiguration Configuration,
        GlobalConfiguration? GlobalConfiguration,
        string BaseDirectory
    )
    {
        // Internal Properties
        internal List<(string FileNameHint, string Content)> Outputs { get; } = new();
        internal List<Diagnostic> Diagnostics { get; } = new();

        // Public Methods
        public void EmitOutput(string fileNameHint, string content)
            => Outputs.Add((fileNameHint, content));

        public void EmitDiagnostic(Diagnostic diagnostic) => Diagnostics.Add(diagnostic);

        /// <summary>
        /// Gets the outputs and diagnostics generated by the generator using this context.
        /// </summary>
        /// <returns>Outputs and diagnostics generated by the generator using this context.</returns>
        public (List<(string FileNameHint, string Content)> Outputs, List<Diagnostic>) GetResult()
            => (Outputs, Diagnostics);
    }
}