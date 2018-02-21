﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Roslynator.CSharp.EmbeddedStatementHelper;

namespace Roslynator.CSharp.Refactorings
{
    internal static class AddBracesRefactoring
    {
        public static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            if (!ifStatement.IsSimpleIf())
                return;

            StatementSyntax statement = GetEmbeddedStatement(ifStatement);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(ifStatement))
                return;

            ReportDiagnostic(context, ifStatement, statement);
        }

        public static void AnalyzeCommonForEachStatement(SyntaxNodeAnalysisContext context)
        {
            var forEachStatement = (CommonForEachStatementSyntax)context.Node;

            StatementSyntax statement = GetEmbeddedStatement(forEachStatement);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(forEachStatement))
                return;

            ReportDiagnostic(context, forEachStatement, statement);
        }

        public static void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
        {
            var forStatement = (ForStatementSyntax)context.Node;

            StatementSyntax statement = GetEmbeddedStatement(forStatement);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(forStatement))
                return;

            ReportDiagnostic(context, forStatement, statement);
        }

        public static void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax)context.Node;

            StatementSyntax statement = GetEmbeddedStatement(usingStatement, allowUsingStatement: false);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(usingStatement))
                return;

            ReportDiagnostic(context, usingStatement, statement);
        }

        public static void AnalyzeWhileStatement(SyntaxNodeAnalysisContext context)
        {
            var whileStatement = (WhileStatementSyntax)context.Node;

            StatementSyntax statement = GetEmbeddedStatement(whileStatement);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(whileStatement))
                return;

            ReportDiagnostic(context, whileStatement, statement);
        }

        public static void AnalyzeDoStatement(SyntaxNodeAnalysisContext context)
        {
            var doStatement = (DoStatementSyntax)context.Node;

            StatementSyntax statement = GetEmbeddedStatement(doStatement);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(doStatement))
                return;

            ReportDiagnostic(context, doStatement, statement);
        }

        public static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
        {
            var lockStatement = (LockStatementSyntax)context.Node;

            StatementSyntax statement = GetEmbeddedStatement(lockStatement);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(lockStatement))
                return;

            ReportDiagnostic(context, lockStatement, statement);
        }

        public static void AnalyzeFixedStatement(SyntaxNodeAnalysisContext context)
        {
            var fixedStatement = (FixedStatementSyntax)context.Node;

            StatementSyntax statement = GetEmbeddedStatement(fixedStatement);

            if (statement == null)
                return;

            if (statement.IsSingleLine() && FormattingSupportsEmbeddedStatement(fixedStatement))
                return;

            ReportDiagnostic(context, fixedStatement, statement);
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, StatementSyntax statement, StatementSyntax embeddedStatement)
        {
            context.ReportDiagnostic(
                DiagnosticDescriptors.AddBracesWhenExpressionSpansOverMultipleLines,
                embeddedStatement,
                CSharpFacts.GetTitle(statement));
        }

        public static Task<Document> RefactorAsync(
            Document document,
            StatementSyntax statement,
            CancellationToken cancellationToken)
        {
            BlockSyntax block = SyntaxFactory.Block(statement).WithFormatterAnnotation();

            return document.ReplaceNodeAsync(statement, block, cancellationToken);
        }
    }
}
