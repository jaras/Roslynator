﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class UseCoalesceExpressionAnalysis
    {
        public static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            if (!ifStatement.IsSimpleIf())
                return;

            if (ifStatement.ContainsDiagnostics)
                return;

            if (ifStatement.SpanContainsDirectives())
                return;

            if (!ifStatement.TryGetContainingList(out SyntaxList<StatementSyntax> statements))
                return;

            if (IsPartOfLazyInitialization(ifStatement, statements))
                return;

            NullCheckExpressionInfo nullCheck = SyntaxInfo.NullCheckExpressionInfo(ifStatement.Condition, semanticModel: context.SemanticModel, cancellationToken: context.CancellationToken);

            if (!nullCheck.Success)
                return;

            SimpleAssignmentStatementInfo simpleAssignment = SyntaxInfo.SimpleAssignmentStatementInfo(ifStatement.SingleNonBlockStatementOrDefault());

            if (!simpleAssignment.Success)
                return;

            if (!CSharpFactory.AreEquivalent(simpleAssignment.Left, nullCheck.Expression))
                return;

            if (!simpleAssignment.Right.IsSingleLine())
                return;

            int index = statements.IndexOf(ifStatement);

            if (index > 0)
            {
                StatementSyntax previousStatement = statements[index - 1];

                if (!previousStatement.ContainsDiagnostics
                    && CanRefactor(previousStatement, ifStatement, nullCheck.Expression, ifStatement.Parent))
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.UseCoalesceExpression, previousStatement);
                }
            }

            if (index == statements.Count - 1)
                return;

            StatementSyntax nextStatement = statements[index + 1];

            if (nextStatement.ContainsDiagnostics)
                return;

            MemberInvocationStatementInfo invocationInfo = SyntaxInfo.MemberInvocationStatementInfo(nextStatement);

            if (!invocationInfo.Success)
                return;

            if (!CSharpFactory.AreEquivalent(nullCheck.Expression, invocationInfo.Expression))
                return;

            if (ifStatement.Parent.ContainsDirectives(TextSpan.FromBounds(ifStatement.SpanStart, nextStatement.Span.End)))
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.InlineLazyInitialization, ifStatement);
        }

        private static bool IsPartOfLazyInitialization(IfStatementSyntax ifStatement, SyntaxList<StatementSyntax> statements)
        {
            return statements.Count == 2
                && statements.IndexOf(ifStatement) == 0
                && statements[1].IsKind(SyntaxKind.ReturnStatement);
        }

        private static bool CanRefactor(
            StatementSyntax statement,
            IfStatementSyntax ifStatement,
            ExpressionSyntax expression,
            SyntaxNode parent)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                    return CanRefactor((LocalDeclarationStatementSyntax)statement, ifStatement, expression, parent);
                case SyntaxKind.ExpressionStatement:
                    return CanRefactor((ExpressionStatementSyntax)statement, ifStatement, expression, parent);
                default:
                    return false;
            }
        }

        private static bool CanRefactor(
            LocalDeclarationStatementSyntax localDeclarationStatement,
            IfStatementSyntax ifStatement,
            ExpressionSyntax expression,
            SyntaxNode parent)
        {
            VariableDeclaratorSyntax declarator = localDeclarationStatement
                .Declaration?
                .Variables
                .SingleOrDefault(shouldThrow: false);

            if (declarator != null)
            {
                ExpressionSyntax value = declarator.Initializer?.Value;

                return value != null
                    && expression.IsKind(SyntaxKind.IdentifierName)
                    && string.Equals(declarator.Identifier.ValueText, ((IdentifierNameSyntax)expression).Identifier.ValueText, StringComparison.Ordinal)
                    && !parent.ContainsDirectives(TextSpan.FromBounds(value.Span.End, ifStatement.SpanStart));
            }

            return false;
        }

        private static bool CanRefactor(
            ExpressionStatementSyntax expressionStatement,
            IfStatementSyntax ifStatement,
            ExpressionSyntax expression,
            SyntaxNode parent)
        {
            ExpressionSyntax expression2 = expressionStatement.Expression;

            if (expression2?.Kind() == SyntaxKind.SimpleAssignmentExpression)
            {
                var assignment = (AssignmentExpressionSyntax)expression2;

                ExpressionSyntax left = assignment.Left;

                if (left?.IsMissing == false)
                {
                    ExpressionSyntax right = assignment.Right;

                    return right?.IsMissing == false
                        && CSharpFactory.AreEquivalent(expression, left)
                        && !parent.ContainsDirectives(TextSpan.FromBounds(right.Span.End, ifStatement.SpanStart));
                }
            }

            return false;
        }
    }
}
