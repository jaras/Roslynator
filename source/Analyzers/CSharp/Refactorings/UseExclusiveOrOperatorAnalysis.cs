﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class UseExclusiveOrOperatorAnalysis
    {
        public static void AnalyzeLogicalOrExpression(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode node = context.Node;

            if (node.ContainsDiagnostics)
                return;

            if (node.SpanContainsDirectives())
                return;

            BinaryExpressionInfo info = SyntaxInfo.BinaryExpressionInfo((BinaryExpressionSyntax)context.Node);

            if (!info.Success)
                return;

            if (!info.Left.IsKind(SyntaxKind.LogicalAndExpression))
                return;

            if (!info.Right.IsKind(SyntaxKind.LogicalAndExpression))
                return;

            ExpressionPair expressions = GetExpressionPair((BinaryExpressionSyntax)info.Left);

            if (!expressions.IsValid)
                return;

            ExpressionPair expressions2 = GetExpressionPair((BinaryExpressionSyntax)info.Right);

            if (!expressions2.IsValid)
                return;

            if (expressions.Expression.Kind() != expressions2.NegatedExpression.Kind())
                return;

            if (expressions.NegatedExpression.Kind() != expressions2.Expression.Kind())
                return;

            if (!AreEquivalent(expressions.Expression, expressions2.NegatedExpression))
                return;

            if (!AreEquivalent(expressions.NegatedExpression, expressions2.Expression))
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.UseExclusiveOrOperator, context.Node);
        }

        private static ExpressionPair GetExpressionPair(BinaryExpressionSyntax logicalAnd)
        {
            BinaryExpressionInfo info = SyntaxInfo.BinaryExpressionInfo(logicalAnd);

            if (info.Success)
            {
                ExpressionSyntax left = info.Left;
                ExpressionSyntax right = info.Right;

                if (left.Kind() == SyntaxKind.LogicalNotExpression)
                {
                    if (right.Kind() != SyntaxKind.LogicalNotExpression)
                    {
                        var logicalNot = (PrefixUnaryExpressionSyntax)left;

                        return new ExpressionPair(right, logicalNot.Operand.WalkDownParentheses());
                    }
                }
                else if (right.Kind() == SyntaxKind.LogicalNotExpression)
                {
                    var logicalNot = (PrefixUnaryExpressionSyntax)right;

                    return new ExpressionPair(left, logicalNot.Operand.WalkDownParentheses());
                }
            }

            return default(ExpressionPair);
        }

        private readonly struct ExpressionPair
        {
            public ExpressionPair(ExpressionSyntax expression, ExpressionSyntax negatedExpression)
            {
                Expression = expression;
                NegatedExpression = negatedExpression;
            }

            public bool IsValid
            {
                get { return Expression != null && NegatedExpression != null; }
            }

            public ExpressionSyntax Expression { get; }
            public ExpressionSyntax NegatedExpression { get; }
        }
    }
}
