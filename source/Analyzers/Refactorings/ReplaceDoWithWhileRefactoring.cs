﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ReplaceDoWithWhileRefactoring
    {
        public static void AnalyzeDoStatement(SyntaxNodeAnalysisContext context)
        {
            var doStatement = (DoStatementSyntax)context.Node;

            if (doStatement.Condition?.Kind() == SyntaxKind.TrueLiteralExpression)
            {
                context.ReportDiagnostic(
                    DiagnosticDescriptors.AvoidUsageOfDoStatementToCreateInfiniteLoop,
                    doStatement.DoKeyword);
            }
        }

        public static Task<Document> RefactorAsync(
            Document document,
            DoStatementSyntax doStatement,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTriviaList trailingTrivia = doStatement.Statement
                .GetTrailingTrivia()
                .AddRange(doStatement.CloseParenToken.TrailingTrivia)
                .AddRange(doStatement.SemicolonToken.LeadingTrivia)
                .AddRange(doStatement.SemicolonToken.TrailingTrivia);

            WhileStatementSyntax newNode = WhileStatement(
                doStatement.WhileKeyword.WithLeadingTrivia(doStatement.DoKeyword.LeadingTrivia),
                doStatement.OpenParenToken,
                doStatement.Condition,
                doStatement.CloseParenToken.WithTrailingTrivia(doStatement.DoKeyword.TrailingTrivia),
                doStatement.Statement.WithTrailingTrivia(trailingTrivia));

            newNode = newNode.WithFormatterAnnotation();

            return document.ReplaceNodeAsync(doStatement, newNode, cancellationToken);
        }
    }
}
