﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactoring
{
    internal static class ReturnStatementRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, ReturnStatementSyntax returnStatement)
        {
            if (context.SupportsSemanticModel)
            {
                if (returnStatement.Expression != null)
                {
                    if (context.Settings.IsAnyRefactoringEnabled(
                        RefactoringIdentifiers.AddBooleanComparison,
                        RefactoringIdentifiers.ChangeMemberTypeAccordingToReturnExpression,
                        RefactoringIdentifiers.AddCastExpression))
                    {
                        await ReturnExpressionRefactoring.ComputeRefactoringsAsync(context, returnStatement.Expression).ConfigureAwait(false);
                    }

                    if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceReturnStatementWithIfStatement)
                        && !returnStatement.Expression.IsBooleanLiteralExpression())
                    {
                        SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                        ITypeSymbol expressionSymbol = semanticModel
                            .GetTypeInfo(returnStatement.Expression, context.CancellationToken)
                            .ConvertedType;

                        if (expressionSymbol?.SpecialType == SpecialType.System_Boolean)
                        {
                            context.RegisterRefactoring(
                                "Replace return statement with if statement",
                                cancellationToken =>
                                {
                                    return ReplaceReturnStatementWithIfStatementRefactoring.RefactorAsync(
                                        context.Document,
                                        returnStatement,
                                        cancellationToken);
                                });
                        }
                    }
                }
                else if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.AddDefaultValueToReturnStatement))
                {
                    await AddDefaultValueToReturnStatementRefactoring.ComputeRefactoringsAsync(context, returnStatement);
                }
            }
        }
    }
}
