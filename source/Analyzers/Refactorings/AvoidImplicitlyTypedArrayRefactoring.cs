﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Refactorings
{
    internal static class AvoidImplicitlyTypedArrayRefactoring
    {
        public static void AnalyzeImplicitArrayCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var expression = (ImplicitArrayCreationExpressionSyntax)context.Node;

            if (expression.ContainsDiagnostics)
                return;

            if (expression.NewKeyword.ContainsDirectives)
                return;

            if (expression.OpenBracketToken.ContainsDirectives)
                return;

            if (expression.CloseBracketToken.ContainsDirectives)
                return;

            if (!(context.SemanticModel.GetTypeSymbol(expression, context.CancellationToken) is IArrayTypeSymbol arrayTypeSymbol))
                return;

            if (!arrayTypeSymbol.ElementType.SupportsExplicitDeclaration())
                return;

            context.ReportDiagnostic(
                DiagnosticDescriptors.AvoidImplicitlyTypedArray,
                Location.Create(expression.SyntaxTree, TextSpan.FromBounds(expression.NewKeyword.SpanStart, expression.CloseBracketToken.Span.End)));
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            ImplicitArrayCreationExpressionSyntax expression,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(expression, cancellationToken);

            var arrayType = (ArrayTypeSyntax)typeSymbol.ToMinimalTypeSyntax(semanticModel, expression.SpanStart);

            SyntaxToken newKeyword = expression.NewKeyword;

            if (!newKeyword.HasTrailingTrivia)
                newKeyword = newKeyword.WithTrailingTrivia(SyntaxFactory.Space);

            ArrayCreationExpressionSyntax newNode = SyntaxFactory.ArrayCreationExpression(
                newKeyword,
                arrayType
                    .WithLeadingTrivia(expression.OpenBracketToken.LeadingTrivia)
                    .WithTrailingTrivia(expression.CloseBracketToken.TrailingTrivia),
                expression.Initializer);

            newNode = newNode.WithFormatterAnnotation();

            return await document.ReplaceNodeAsync(expression, newNode, cancellationToken).ConfigureAwait(false);
        }
    }
}
