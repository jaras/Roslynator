﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings.IntroduceAndInitialize;
using Roslynator.CSharp.Refactorings.NodeInList;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ParameterListRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, ParameterListSyntax parameterList)
        {
            SeparatedSyntaxList<ParameterSyntax> parameters = parameterList.Parameters;

            if (parameters.Count == 0)
                return;

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.DuplicateParameter))
            {
                var refactoring = new DuplicateParameterRefactoring(parameterList);
                refactoring.ComputeRefactoring(context);
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.CheckParameterForNull))
                await CheckParameterForNullRefactoring.ComputeRefactoringAsync(context, parameterList).ConfigureAwait(false);

            if (context.IsAnyRefactoringEnabled(
                RefactoringIdentifiers.IntroduceAndInitializeField,
                RefactoringIdentifiers.IntroduceAndInitializeProperty))
            {
                IntroduceAndInitializeRefactoring.ComputeRefactoring(context, parameterList);
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.FormatParameterList)
                && (context.Span.IsEmpty || context.Span.IsBetweenSpans(parameterList)))
            {
                if (parameterList.IsSingleLine())
                {
                    if (parameters.Count > 1)
                    {
                        context.RegisterRefactoring(
                            "Format each parameter on a separate line",
                            cancellationToken => FormatParameterListRefactoring.FormatEachParameterOnSeparateLineAsync(context.Document, parameterList, cancellationToken));
                    }
                }
                else
                {
                    string title = parameters.Count == 1
                        ? "Format parameter on a single line"
                        : "Format all parameters on a single line";

                    context.RegisterRefactoring(
                        title,
                        cancellationToken => FormatParameterListRefactoring.FormatAllParametersOnSingleLineAsync(context.Document, parameterList, cancellationToken));
                }
            }
        }
    }
}