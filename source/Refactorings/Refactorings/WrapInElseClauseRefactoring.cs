﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class WrapInElseClauseRefactoring
    {
        public static void ComputeRefactoring(RefactoringContext context, StatementsSelection selectedStatements)
        {
            StatementSyntax lastStatement = selectedStatements.Last();

            if (lastStatement.IsKind(SyntaxKind.ReturnStatement)
                && selectedStatements.LastIndex == selectedStatements.UnderlyingList.IndexOf(lastStatement)
                && selectedStatements.FirstIndex > 0)
            {
                var returnStatement = (ReturnStatementSyntax)lastStatement;

                ExpressionSyntax expression = returnStatement.Expression;

                if (expression != null)
                {
                    StatementSyntax prevStatement = selectedStatements.UnderlyingList[selectedStatements.FirstIndex - 1];

                    if (prevStatement.IsKind(SyntaxKind.IfStatement))
                    {
                        var ifStatement = (IfStatementSyntax)prevStatement;

                        IfStatementInfo ifStatementInfo = SyntaxInfo.IfStatementInfo(ifStatement);

                        foreach (IfStatementOrElseClause ifOrElse in ifStatementInfo)
                        {
                            if (ifOrElse.IsElse)
                                return;

                            if (!IsLastStatementReturnStatement(ifOrElse))
                                return;
                        }

                        context.RegisterRefactoring(
                            "Wrap in else clause",
                            cancellationToken => RefactorAsync(context.Document, ifStatementInfo, selectedStatements, cancellationToken));
                    }
                }
            }
        }

        private static bool IsLastStatementReturnStatement(IfStatementSyntax ifStatement)
        {
            StatementSyntax statement = ifStatement.Statement;

            if (statement.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)statement;

                return IsReturnStatementWithExpression(block.Statements.LastOrDefault());
            }
            else
            {
                return IsReturnStatementWithExpression(statement);
            }
        }

        private static bool IsReturnStatementWithExpression(StatementSyntax statement)
        {
            if (statement?.Kind() == SyntaxKind.ReturnStatement)
            {
                var returnStatement = (ReturnStatementSyntax)statement;

                return returnStatement.Expression != null;
            }

            return false;
        }

        private static Task<Document> RefactorAsync(
            Document document,
            IfStatementInfo ifStatementInfo,
            StatementsSelection selectedStatements,
            CancellationToken cancellationToken)
        {
            StatementSyntax newStatement = null;

            if (selectedStatements.Count == 1
                && !ifStatementInfo.Any(f => f.Statement?.Kind() == SyntaxKind.Block))
            {
                newStatement = selectedStatements.First();
            }
            else
            {
                newStatement = SyntaxFactory.Block(selectedStatements);
            }

            ElseClauseSyntax elseClause = SyntaxFactory.ElseClause(newStatement).WithFormatterAnnotation();

            IfStatementSyntax lastIfStatement = ifStatementInfo.Last();

            IfStatementSyntax ifStatement = ifStatementInfo.IfStatement;

            IfStatementSyntax newIfStatement = ifStatement.ReplaceNode(
                lastIfStatement,
                lastIfStatement.WithElse(elseClause));

            SyntaxList<StatementSyntax> newStatements = selectedStatements.UnderlyingList.Replace(ifStatement, newIfStatement);

            for (int i = newStatements.Count - 1; i >= selectedStatements.FirstIndex; i--)
                newStatements = newStatements.RemoveAt(i);

            return document.ReplaceStatementsAsync(SyntaxInfo.StatementsInfo(selectedStatements), newStatements, cancellationToken);
        }
    }
}