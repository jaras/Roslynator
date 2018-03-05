﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace Roslynator.CSharp.Refactorings.RemoveRedundantStatement
{
    internal class RemoveRedundantYieldBreakStatementRefactoring : RemoveRedundantStatementRefactoring<YieldStatementSyntax>
    {
        protected override bool IsFixable(StatementSyntax statement, BlockSyntax block, SyntaxKind parentKind)
        {
            if (!parentKind.Is(
                SyntaxKind.MethodDeclaration,
                SyntaxKind.LocalFunctionStatement))
            {
                return false;
            }

            //TODO: test
            if (object.ReferenceEquals(block.Statements.SingleOrDefault(shouldThrow: false), statement))
                return false;

            TextSpan span = TextSpan.FromBounds(block.SpanStart, statement.FullSpan.Start);

            return block.ContainsYield(span);
        }
    }
}
