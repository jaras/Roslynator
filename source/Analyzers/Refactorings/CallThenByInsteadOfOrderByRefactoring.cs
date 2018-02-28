﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class CallThenByInsteadOfOrderByRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, MemberInvocationExpressionInfo invocationInfo)
        {
            ExpressionSyntax expression = invocationInfo.Expression;

            if (expression.Kind() != SyntaxKind.InvocationExpression)
                return;

            MemberInvocationExpressionInfo invocationInfo2 = SyntaxInfo.MemberInvocationExpressionInfo((InvocationExpressionSyntax)expression);

            if (!invocationInfo2.Success)
                return;

            if (!StringUtility.Equals(invocationInfo2.NameText, "OrderBy", "OrderByDescending"))
                return;

            if (IsOrderByOrOrderByDescending(invocationInfo.InvocationExpression, context.SemanticModel, context.CancellationToken)
                && IsOrderByOrOrderByDescending(invocationInfo2.InvocationExpression, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(
                    DiagnosticDescriptors.CallThenByInsteadOfOrderBy,
                    invocationInfo.Name,
                    (invocationInfo.NameText == "OrderByDescending") ? "Descending" : null);
            }
        }

        private static bool IsOrderByOrOrderByDescending(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            IMethodSymbol methodSymbol = semanticModel.GetExtensionMethodInfo(invocationExpression, cancellationToken).Symbol;

            return methodSymbol?.IsName("OrderBy", "OrderByDescending") == true
                && SymbolUtility.IsLinqExtensionOfIEnumerableOfT(methodSymbol, semanticModel);
        }

        public static Task<Document> RefactorAsync(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            string newName,
            CancellationToken cancellationToken)
        {
            InvocationExpressionSyntax newInvocationExpression = RefactoringUtility.ChangeInvokedMethodName(invocationExpression, newName);

            return document.ReplaceNodeAsync(invocationExpression, newInvocationExpression, cancellationToken);
        }
    }
}
