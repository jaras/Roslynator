﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class UseConditionalAccessRefactoring
    {
        public static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context, INamedTypeSymbol expressionType)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            if (!ifStatement.IsSimpleIf())
                return;

            if (ifStatement.ContainsDiagnostics)
                return;

            if (ifStatement.SpanContainsDirectives())
                return;

            NullCheckExpressionInfo nullCheck = SyntaxInfo.NullCheckExpressionInfo(ifStatement.Condition, allowedStyles: NullCheckStyles.NotEqualsToNull);

            if (!nullCheck.Success)
                return;

            MemberInvocationStatementInfo invocationInfo = SyntaxInfo.MemberInvocationStatementInfo(ifStatement.SingleNonBlockStatementOrDefault());

            if (!invocationInfo.Success)
                return;

            if (!CSharpFactory.AreEquivalent(nullCheck.Expression, invocationInfo.Expression))
                return;

            if (ifStatement.IsInExpressionTree(expressionType, context.SemanticModel, context.CancellationToken))
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.UseConditionalAccess, ifStatement);
        }

        public static void AnalyzeLogicalAndExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol expressionType)
        {
            var logicalAndExpression = (BinaryExpressionSyntax)context.Node;

            if (logicalAndExpression.ContainsDiagnostics)
                return;

            ExpressionSyntax expression = SyntaxInfo.NullCheckExpressionInfo(logicalAndExpression.Left, allowedStyles: NullCheckStyles.NotEqualsToNull).Expression;

            if (expression == null)
                return;

            if (context.SemanticModel
                .GetTypeSymbol(expression, context.CancellationToken)?
                .IsReferenceType != true)
            {
                return;
            }

            ExpressionSyntax right = logicalAndExpression.Right?.WalkDownParentheses();

            if (right == null)
                return;

            if (!ValidateRightExpression(right, context.SemanticModel, context.CancellationToken))
                return;

            if (RefactoringUtility.ContainsOutArgumentWithLocal(right, context.SemanticModel, context.CancellationToken))
                return;

            ExpressionSyntax expression2 = FindExpressionThatCanBeConditionallyAccessed(expression, right);

            if (expression2?.SpanContainsDirectives() != false)
                return;

            if (logicalAndExpression.IsInExpressionTree(expressionType, context.SemanticModel, context.CancellationToken))
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.UseConditionalAccess, logicalAndExpression);
        }

        internal static ExpressionSyntax FindExpressionThatCanBeConditionallyAccessed(ExpressionSyntax expressionToFind, ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.LogicalNotExpression))
                expression = ((PrefixUnaryExpressionSyntax)expression).Operand;

            SyntaxKind kind = expressionToFind.Kind();

            SyntaxToken firstToken = expression.GetFirstToken();

            int start = firstToken.SpanStart;

            SyntaxNode node = firstToken.Parent;

            while (node?.SpanStart == start)
            {
                if (kind == node.Kind()
                    && node.IsParentKind(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.ElementAccessExpression)
                    && CSharpFactory.AreEquivalent(expressionToFind, node))
                {
                    return (ExpressionSyntax)node;
                }

                node = node.Parent;
            }

            return null;
        }

        private static bool ValidateRightExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.EqualsExpression:
                    {
                        return ((BinaryExpressionSyntax)expression)
                            .Right?
                            .WalkDownParentheses()
                            .HasConstantNonNullValue(semanticModel, cancellationToken) == true;
                    }
                case SyntaxKind.NotEqualsExpression:
                    {
                        return ((BinaryExpressionSyntax)expression)
                            .Right?
                            .WalkDownParentheses()
                            .Kind() == SyntaxKind.NullLiteralExpression;
                    }
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.ElementAccessExpression:
                case SyntaxKind.LogicalNotExpression:
                case SyntaxKind.IsExpression:
                case SyntaxKind.IsPatternExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.LogicalAndExpression:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        private static bool HasConstantNonNullValue(this ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            Optional<object> optional = semanticModel.GetConstantValue(expression, cancellationToken);

            return optional.HasValue
                && optional.Value != null;
        }

        public static Task<Document> RefactorAsync(
            Document document,
            BinaryExpressionSyntax logicalAnd,
            CancellationToken cancellationToken)
        {
            ExpressionSyntax newNode = CreateExpressionWithConditionalAccess(logicalAnd)
                .WithLeadingTrivia(logicalAnd.GetLeadingTrivia())
                .WithFormatterAnnotation()
                .Parenthesize();

            return document.ReplaceNodeAsync(logicalAnd, newNode, cancellationToken);
        }

        private static ExpressionSyntax CreateExpressionWithConditionalAccess(BinaryExpressionSyntax logicalAnd)
        {
            ExpressionSyntax expression = SyntaxInfo.NullCheckExpressionInfo(logicalAnd.Left, allowedStyles: NullCheckStyles.NotEqualsToNull).Expression;

            ExpressionSyntax right = logicalAnd.Right?.WalkDownParentheses();

            ExpressionSyntax expression2 = FindExpressionThatCanBeConditionallyAccessed(
                expression,
                right);

            SyntaxKind kind = right.Kind();

            if (kind == SyntaxKind.LogicalNotExpression)
            {
                var logicalNot = (PrefixUnaryExpressionSyntax)right;
                ExpressionSyntax operand = logicalNot.Operand;

                string s = operand.ToFullString();

                int length = expression2.Span.End - operand.FullSpan.Start;
                int trailingLength = operand.GetTrailingTrivia().Span.Length;

                var sb = new StringBuilder();
                sb.Append(s, 0, length);
                sb.Append("?");
                sb.Append(s, length, s.Length - length - trailingLength);
                sb.Append(" == false");
                sb.Append(s, s.Length - trailingLength, trailingLength);

                return SyntaxFactory.ParseExpression(sb.ToString());
            }
            else
            {
                string s = right.ToFullString();

                int length = expression2.Span.End - right.FullSpan.Start;
                int trailingLength = right.GetTrailingTrivia().Span.Length;

                var sb = new StringBuilder();
                sb.Append(s, 0, length);
                sb.Append("?");
                sb.Append(s, length, s.Length - length - trailingLength);

                switch (kind)
                {
                    case SyntaxKind.LogicalOrExpression:
                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.BitwiseOrExpression:
                    case SyntaxKind.BitwiseAndExpression:
                    case SyntaxKind.ExclusiveOrExpression:
                    case SyntaxKind.EqualsExpression:
                    case SyntaxKind.NotEqualsExpression:
                    case SyntaxKind.LessThanExpression:
                    case SyntaxKind.LessThanOrEqualExpression:
                    case SyntaxKind.GreaterThanExpression:
                    case SyntaxKind.GreaterThanOrEqualExpression:
                    case SyntaxKind.IsExpression:
                    case SyntaxKind.AsExpression:
                    case SyntaxKind.IsPatternExpression:
                        break;
                    default:
                        {
                            sb.Append(" == true");
                            break;
                        }
                }

                sb.Append(s, s.Length - trailingLength, trailingLength);

                return SyntaxFactory.ParseExpression(sb.ToString());
            }
        }

        public static Task<Document> RefactorAsync(
            Document document,
            IfStatementSyntax ifStatement,
            CancellationToken cancellationToken)
        {
            var statement = (ExpressionStatementSyntax)ifStatement.SingleNonBlockStatementOrDefault();

            MemberInvocationStatementInfo invocationInfo = SyntaxInfo.MemberInvocationStatementInfo(statement);

            int insertIndex = invocationInfo.Expression.Span.End - statement.FullSpan.Start;
            StatementSyntax newStatement = SyntaxFactory.ParseStatement(statement.ToFullString().Insert(insertIndex, "?"));

            IEnumerable<SyntaxTrivia> leading = ifStatement.DescendantTrivia(TextSpan.FromBounds(ifStatement.SpanStart, statement.SpanStart));

            newStatement = (leading.All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                ? newStatement.WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                : newStatement.WithLeadingTrivia(ifStatement.GetLeadingTrivia().Concat(leading));

            IEnumerable<SyntaxTrivia> trailing = ifStatement.DescendantTrivia(TextSpan.FromBounds(statement.Span.End, ifStatement.Span.End));

            newStatement = (leading.All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                ? newStatement.WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                : newStatement.WithTrailingTrivia(trailing.Concat(ifStatement.GetTrailingTrivia()));

            return document.ReplaceNodeAsync(ifStatement, newStatement, cancellationToken);
        }
    }
}
