﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Roslynator.CSharp.Refactorings.ParameterNameDiffersFromBaseAnalysis;

namespace Roslynator.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParameterNameDiffersFromBaseAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.ParameterNameDiffersFromBase); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeMethodSymbol, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzePropertySymbol, SymbolKind.Property);
        }
    }
}
