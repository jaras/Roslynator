﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class SimplifyLinqMethodChainRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, MemberInvocationExpressionInfo invocationInfo)
        {
            MemberInvocationExpressionInfo invocationInfo2 = SyntaxInfo.MemberInvocationExpressionInfo(invocationInfo.Expression);

            if (!invocationInfo2.Success)
                return;

            if (invocationInfo2.Arguments.Count != 1)
                return;

            if (invocationInfo2.NameText != "Where")
                return;

            InvocationExpressionSyntax invocation = invocationInfo.InvocationExpression;

            SemanticModel semanticModel = context.SemanticModel;
            CancellationToken cancellationToken = context.CancellationToken;

            IMethodSymbol methodSymbol = semanticModel.GetExtensionMethodInfo(invocation, cancellationToken).Symbol;

            if (methodSymbol == null)
                return;

            if (!SymbolUtility.IsLinqExtensionOfIEnumerableOfTWithoutParameters(methodSymbol, invocationInfo.NameText, semanticModel))
                return;

            IMethodSymbol methodSymbol2 = semanticModel.GetExtensionMethodInfo(invocationInfo2.InvocationExpression, cancellationToken).Symbol;

            if (methodSymbol2 == null)
                return;

            if (!SymbolUtility.IsLinqWhere(methodSymbol2, semanticModel, allowImmutableArrayExtension: true))
                return;

            TextSpan span = TextSpan.FromBounds(invocationInfo2.Name.Span.Start, invocation.Span.End);

            if (invocation.ContainsDirectives(span))
                return;

            context.ReportDiagnostic(
                DiagnosticDescriptors.SimplifyLinqMethodChain,
                Location.Create(invocation.SyntaxTree, span));
        }

        public static Task<Document> RefactorAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

            var invocation2 = (InvocationExpressionSyntax)memberAccess.Expression;

            var memberAccess2 = (MemberAccessExpressionSyntax)invocation2.Expression;

            InvocationExpressionSyntax newNode = invocation2.WithExpression(
                memberAccess2.WithName(memberAccess.Name.WithTriviaFrom(memberAccess2.Name)));

            IEnumerable<SyntaxTrivia> trivia = invocation.DescendantTrivia(TextSpan.FromBounds(invocation2.Span.End, invocation.Span.End));

            if (trivia.Any(f => !f.IsWhitespaceOrEndOfLineTrivia()))
            {
                newNode = newNode.WithTrailingTrivia(trivia.Concat(invocation.GetTrailingTrivia()));
            }
            else
            {
                newNode = newNode.WithTrailingTrivia(invocation.GetTrailingTrivia());
            }

            return document.ReplaceNodeAsync(invocation, newNode, cancellationToken);
        }
    }
}
