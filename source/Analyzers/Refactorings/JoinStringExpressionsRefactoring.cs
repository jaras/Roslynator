﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class JoinStringExpressionsRefactoring
    {
        public static void AnalyzeAddExpression(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode node = context.Node;

            if (node.ContainsDiagnostics)
                return;

            if (node.SpanContainsDirectives())
                return;

            var addExpression = (BinaryExpressionSyntax)node;

            StringConcatenationExpressionInfo concatenationInfo = SyntaxInfo.StringConcatenationExpressionInfo(addExpression, context.SemanticModel, context.CancellationToken);

            if (!concatenationInfo.Success)
                return;

            StringConcatenationAnalysis analysis = concatenationInfo.Analyze();

            if (!analysis.ContainsUnspecifiedExpression
                && (analysis.ContainsStringLiteral ^ analysis.ContainsInterpolatedString)
                && (analysis.ContainsNonVerbatimExpression ^ analysis.ContainsVerbatimExpression)
                && (analysis.ContainsVerbatimExpression || addExpression.IsSingleLine(includeExteriorTrivia: false, cancellationToken: context.CancellationToken)))
            {
                context.ReportDiagnostic(DiagnosticDescriptors.JoinStringExpressions, addExpression);
            }
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            BinaryExpressionSyntax binaryExpression,
            CancellationToken cancellationToken)
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            StringConcatenationExpressionInfo concatenationInfo = SyntaxInfo.StringConcatenationExpressionInfo(binaryExpression, semanticModel, cancellationToken);

            ExpressionSyntax newNode = null;

            StringConcatenationAnalysis analysis = concatenationInfo.Analyze();

            if (analysis.ContainsStringLiteral)
            {
                if (analysis.ContainsVerbatimExpression
                    && concatenationInfo.ContainsMultiLineExpression())
                {
                    newNode = concatenationInfo.ToMultiLineStringLiteral();
                }
                else
                {
                    newNode = concatenationInfo.ToStringLiteral();
                }
            }
            else
            {
                newNode = concatenationInfo.ToInterpolatedString();
            }

            newNode = newNode.WithTriviaFrom(binaryExpression);

            return await document.ReplaceNodeAsync(binaryExpression, newNode, cancellationToken).ConfigureAwait(false);
        }
    }
}
