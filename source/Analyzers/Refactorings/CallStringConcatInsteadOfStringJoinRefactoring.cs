﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class CallStringConcatInsteadOfStringJoinRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, MemberInvocationExpressionInfo invocationInfo)
        {
            InvocationExpressionSyntax invocation = invocationInfo.InvocationExpression;

            ArgumentSyntax firstArgument = invocationInfo.Arguments.FirstOrDefault();

            if (firstArgument == null)
                return;

            if (invocationInfo.NameText != "Join")
                return;

            if (invocationInfo.MemberAccessExpression.SpanOrTrailingTriviaContainsDirectives()
                || invocationInfo.ArgumentList.OpenParenToken.ContainsDirectives
                || firstArgument.ContainsDirectives)
            {
                return;
            }

            SemanticModel semanticModel = context.SemanticModel;
            CancellationToken cancellationToken = context.CancellationToken;

            IMethodSymbol methodSymbol = semanticModel.GetMethodSymbol(invocation, cancellationToken);

            if (!SymbolUtility.IsPublicStaticNonGenericMethod(methodSymbol, "Join"))
                return;

            if (methodSymbol.ContainingType?.SpecialType != SpecialType.System_String)
                return;

            if (!methodSymbol.IsReturnType(SpecialType.System_String))
                return;

            ImmutableArray<IParameterSymbol> parameters = methodSymbol.Parameters;

            if (parameters.Length != 2)
                return;

            if (parameters[0].Type.SpecialType != SpecialType.System_String)
                return;

            if (!parameters[1].IsParamsOf(SpecialType.System_String, SpecialType.System_Object)
                && !parameters[1].Type.IsConstructedFromIEnumerableOfT())
            {
                return;
            }

            if (firstArgument.Expression == null)
                return;

            if (!CSharpUtility.IsEmptyStringExpression(firstArgument.Expression, semanticModel, cancellationToken))
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.CallStringConcatInsteadOfStringJoin, invocationInfo.Name);
        }

        public static Task<Document> RefactorAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

            MemberAccessExpressionSyntax newMemberAccess = memberAccess.WithName(IdentifierName("Concat").WithTriviaFrom(memberAccess.Name));

            ArgumentListSyntax argumentList = invocation.ArgumentList;
            SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;

            ArgumentListSyntax newArgumentList = argumentList
                .WithArguments(arguments.RemoveAt(0))
                .WithOpenParenToken(argumentList.OpenParenToken.AppendToTrailingTrivia(arguments[0].GetLeadingAndTrailingTrivia()));

            InvocationExpressionSyntax newInvocation = invocation
                .WithExpression(newMemberAccess)
                .WithArgumentList(newArgumentList);

            return document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken);
        }
    }
}
