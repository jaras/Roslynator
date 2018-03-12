﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp;

namespace Roslynator.CSharp.Refactorings
{
    internal static class AvoidUsageOfWhileStatementToCreateInfiniteLoopAnalysis
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, WhileStatementSyntax whileStatement)
        {
            if (whileStatement.Condition?.Kind() != SyntaxKind.TrueLiteralExpression)
                return;

            TextSpan span = TextSpan.FromBounds(
                whileStatement.OpenParenToken.Span.End,
                whileStatement.CloseParenToken.SpanStart);

            if (!whileStatement
                .DescendantTrivia(span)
                .All(f => f.IsWhitespaceOrEndOfLineTrivia()))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.AvoidUsageOfWhileStatementToCreateInfiniteLoop, whileStatement.WhileKeyword);
        }
    }
}
