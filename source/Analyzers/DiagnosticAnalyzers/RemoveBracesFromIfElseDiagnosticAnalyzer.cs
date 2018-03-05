﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveBracesFromIfElseDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.RemoveBracesFromIfElse,
                    DiagnosticDescriptors.RemoveBracesFromIfElseFadeOut);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        }

        private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            if (ifStatement.IsParentKind(SyntaxKind.ElseClause))
                return;

            if (ifStatement.Else == null)
                return;

            BracesAnalysis analysis = BracesAnalysis.AnalyzeBraces(ifStatement);

            if (!analysis.RemoveBraces)
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.RemoveBracesFromIfElse, ifStatement);

            //TODO: test
            foreach (IfStatementOrElseClause ifOrElse in SyntaxInfo.IfStatementInfo(ifStatement))
            {
                if (ifOrElse.Statement is BlockSyntax block)
                    context.ReportBraces(DiagnosticDescriptors.RemoveBracesFromIfElseFadeOut, block);
            }
        }
    }
}