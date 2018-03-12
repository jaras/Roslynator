﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class RemoveRedundantAssignmentAnalysis
    {
        internal static void AnalyzeSimpleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.Node.ContainsDiagnostics)
                return;

            if (context.Node.SpanOrTrailingTriviaContainsDirectives())
                return;

            var assignment = (AssignmentExpressionSyntax)context.Node;

            SimpleAssignmentStatementInfo assignmentInfo = SyntaxInfo.SimpleAssignmentStatementInfo(assignment);

            if (!assignmentInfo.Success)
                return;

            if (!(assignmentInfo.Left is IdentifierNameSyntax identifierName))
                return;

            StatementSyntax nextStatement = assignmentInfo.Statement.NextStatement();

            if (nextStatement == null)
                return;

            if (nextStatement.SpanOrLeadingTriviaContainsDirectives())
                return;

            if (!(nextStatement is ReturnStatementSyntax returnStatement))
                return;

            if (!(returnStatement.Expression?.WalkDownParentheses() is IdentifierNameSyntax identifierName2))
                return;

            if (!string.Equals(identifierName.Identifier.ValueText, identifierName2.Identifier.ValueText, StringComparison.Ordinal))
                return;

            ISymbol symbol = context.SemanticModel.GetSymbol(identifierName, context.CancellationToken);

            if (symbol == null)
                return;

            if (!IsFixableSymbol(symbol))
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.RemoveRedundantAssignment, assignment);
        }

        private static bool IsFixableSymbol(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    return true;
                case SymbolKind.Parameter:
                    return ((IParameterSymbol)symbol).RefKind == RefKind.None;
            }

            return false;
        }
    }
}
