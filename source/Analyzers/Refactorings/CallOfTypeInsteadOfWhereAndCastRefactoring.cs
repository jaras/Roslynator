﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class CallOfTypeInsteadOfWhereAndCastRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, MemberInvocationExpressionInfo invocationInfo)
        {
            MemberInvocationExpressionInfo invocationInfo2 = SyntaxInfo.MemberInvocationExpressionInfo(invocationInfo.Expression);

            if (!invocationInfo2.Success)
                return;

            ArgumentSyntax argument = invocationInfo2.Arguments.SingleOrDefault(shouldThrow: false);

            if (argument == null)
                return;

            if (!string.Equals(invocationInfo2.NameText, "Where", StringComparison.Ordinal))
                return;

            SemanticModel semanticModel = context.SemanticModel;
            CancellationToken cancellationToken = context.CancellationToken;

            IMethodSymbol methodSymbol = semanticModel.GetReducedExtensionMethodInfo(invocationInfo.InvocationExpression, cancellationToken).Symbol;

            if (methodSymbol == null)
                return;

            if (!SymbolUtility.IsLinqCast(methodSymbol, semanticModel))
                return;

            IMethodSymbol methodSymbol2 = semanticModel.GetReducedExtensionMethodInfo(invocationInfo2.InvocationExpression, cancellationToken).Symbol;

            if (methodSymbol2 == null)
                return;

            if (!SymbolUtility.IsLinqWhere(methodSymbol2, semanticModel))
                return;

            IsExpressionInfo isExpressionInfo = SyntaxInfo.IsExpressionInfo(GetLambdaExpression(argument.Expression));

            if (!isExpressionInfo.Success)
                return;

            TypeSyntax type2 = (invocationInfo.Name as GenericNameSyntax)?.TypeArgumentList?.Arguments.SingleOrDefault(shouldThrow: false);

            if (type2 == null)
                return;

            ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(isExpressionInfo.Type, cancellationToken);

            if (typeSymbol == null)
                return;

            ITypeSymbol typeSymbol2 = semanticModel.GetTypeSymbol(type2, cancellationToken);

            if (!typeSymbol.Equals(typeSymbol2))
                return;

            TextSpan span = TextSpan.FromBounds(invocationInfo2.Name.Span.Start, invocationInfo.InvocationExpression.Span.End);

            if (invocationInfo.InvocationExpression.ContainsDirectives(span))
                return;

            context.ReportDiagnostic(
                DiagnosticDescriptors.SimplifyLinqMethodChain,
                Location.Create(invocationInfo.InvocationExpression.SyntaxTree, span));
        }

        private static SyntaxNode GetLambdaExpression(ExpressionSyntax expression)
        {
            CSharpSyntaxNode body = (expression as LambdaExpressionSyntax)?.Body;

            if (body?.Kind() == SyntaxKind.Block)
            {
                StatementSyntax statement = ((BlockSyntax)body).Statements.SingleOrDefault(shouldThrow: false);

                if (statement?.Kind() == SyntaxKind.ReturnStatement)
                {
                    var returnStatement = (ReturnStatementSyntax)statement;

                    return returnStatement.Expression;
                }
            }

            return body;
        }

        public static Task<Document> RefactorAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

            var invocation2 = (InvocationExpressionSyntax)memberAccess.Expression;

            var memberAccess2 = (MemberAccessExpressionSyntax)invocation2.Expression;

            var genericName = (GenericNameSyntax)memberAccess.Name;

            InvocationExpressionSyntax newInvocation = invocation2.Update(
                memberAccess2.WithName(genericName.WithIdentifier(Identifier("OfType"))),
                invocation.ArgumentList.WithArguments(SeparatedList<ArgumentSyntax>()));

            return document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken);
        }
    }
}
