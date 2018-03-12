﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class UseCoalesceExpressionRefactoring
    {
        public static Task<Document> InlineLazyInitializationAsync(
            Document document,
            IfStatementSyntax ifStatement,
            CancellationToken cancellationToken)
        {
            StatementListInfo statementsInfo = SyntaxInfo.StatementListInfo(ifStatement);

            SyntaxList<StatementSyntax> statements = statementsInfo.Statements;

            int index = statements.IndexOf(ifStatement);

            StatementSyntax expressionStatement = (ExpressionStatementSyntax)statements[index + 1];

            MemberInvocationStatementInfo invocationInfo = SyntaxInfo.MemberInvocationStatementInfo((ExpressionStatementSyntax)expressionStatement);

            ExpressionSyntax expression = invocationInfo.Expression;

            SimpleAssignmentStatementInfo assignmentInfo = SyntaxInfo.SimpleAssignmentStatementInfo((ExpressionStatementSyntax)ifStatement.SingleNonBlockStatementOrDefault());

            BinaryExpressionSyntax coalesceExpression = CSharpFactory.CoalesceExpression(expression.WithoutTrivia(), ParenthesizedExpression(assignmentInfo.AssignmentExpression));

            ParenthesizedExpressionSyntax newExpression = ParenthesizedExpression(coalesceExpression)
                .WithTriviaFrom(expression);

            StatementSyntax newExpressionStatement = expressionStatement.ReplaceNode(expression, newExpression);

            IEnumerable<SyntaxTrivia> trivia = statementsInfo.Parent.DescendantTrivia(TextSpan.FromBounds(ifStatement.FullSpan.Start, expressionStatement.FullSpan.Start));

            if (trivia.Any(f => !f.IsWhitespaceOrEndOfLineTrivia()))
                newExpressionStatement = newExpressionStatement.PrependToLeadingTrivia(trivia);

            SyntaxList<StatementSyntax> newStatements = statements
                .Replace(expressionStatement, newExpressionStatement)
                .RemoveAt(index);

            return document.ReplaceStatementsAsync(statementsInfo, newStatements, cancellationToken);
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            StatementSyntax statement,
            CancellationToken cancellationToken)
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            StatementListInfo statementsInfo = SyntaxInfo.StatementListInfo(statement);

            SyntaxList<StatementSyntax> statements = statementsInfo.Statements;

            int index = statements.IndexOf(statement);

            switch (statement.Kind())
            {
                case SyntaxKind.IfStatement:
                    {
                        var ifStatement = (IfStatementSyntax)statement;

                        var expressionStatement = (ExpressionStatementSyntax)ifStatement.SingleNonBlockStatementOrDefault();

                        var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;

                        ExpressionSyntax left = assignment.Left;
                        ExpressionSyntax right = assignment.Right;

                        BinaryExpressionSyntax coalesceExpression = CodeFixesUtility.CreateCoalesceExpression(
                            semanticModel.GetTypeSymbol(left, cancellationToken),
                            left.WithoutLeadingTrivia().WithTrailingTrivia(Space),
                            right.WithLeadingTrivia(Space),
                            ifStatement.SpanStart,
                            semanticModel);

                        AssignmentExpressionSyntax newAssignment = assignment.WithRight(coalesceExpression.WithTriviaFrom(right));

                        ExpressionStatementSyntax newNode = expressionStatement.WithExpression(newAssignment);

                        IEnumerable<SyntaxTrivia> trivia = ifStatement.DescendantTrivia(TextSpan.FromBounds(ifStatement.SpanStart, expressionStatement.SpanStart));

                        if (trivia.All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                        {
                            newNode = newNode.WithLeadingTrivia(ifStatement.GetLeadingTrivia());
                        }
                        else
                        {
                            newNode = newNode
                                .WithLeadingTrivia(ifStatement.GetLeadingTrivia().Concat(trivia))
                                .WithFormatterAnnotation();
                        }

                        return await document.ReplaceNodeAsync(ifStatement, newNode, cancellationToken).ConfigureAwait(false);
                    }
                case SyntaxKind.ExpressionStatement:
                    {
                        var expressionStatement = (ExpressionStatementSyntax)statement;

                        var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;

                        return await RefactorAsync(document, expressionStatement, (IfStatementSyntax)statements[index + 1], index, statementsInfo, assignment.Right, semanticModel, cancellationToken).ConfigureAwait(false);
                    }
                case SyntaxKind.LocalDeclarationStatement:
                    {
                        var localDeclaration = (LocalDeclarationStatementSyntax)statement;

                        ExpressionSyntax value = localDeclaration
                            .Declaration
                            .Variables
                            .First()
                            .Initializer
                            .Value;

                        return await RefactorAsync(document, localDeclaration, (IfStatementSyntax)statements[index + 1], index, statementsInfo, value, semanticModel, cancellationToken).ConfigureAwait(false);
                    }
                default:
                    {
                        Debug.Fail(statement.Kind().ToString());

                        return document;
                    }
            }
        }

        private static Task<Document> RefactorAsync(
            Document document,
            StatementSyntax statement,
            IfStatementSyntax ifStatement,
            int statementIndex,
            StatementListInfo statementsInfo,
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var expressionStatement = (ExpressionStatementSyntax)ifStatement.SingleNonBlockStatementOrDefault();

            var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;

            BinaryExpressionSyntax newNode = CodeFixesUtility.CreateCoalesceExpression(
                semanticModel.GetTypeSymbol(assignment.Left, cancellationToken),
                expression.WithoutTrailingTrivia(),
                assignment.Right.WithTrailingTrivia(expression.GetTrailingTrivia()),
                statement.SpanStart,
                semanticModel);

            StatementSyntax newStatement = statement.ReplaceNode(expression, newNode);

            IEnumerable<SyntaxTrivia> trivia = statementsInfo.Parent.DescendantTrivia(TextSpan.FromBounds(statement.Span.End, ifStatement.Span.End));

            if (!trivia.All(f => f.IsWhitespaceOrEndOfLineTrivia()))
            {
                newStatement = newStatement.WithTrailingTrivia(trivia);
                newStatement = newStatement.AppendToTrailingTrivia(ifStatement.GetTrailingTrivia());
            }
            else
            {
                newStatement = newStatement.WithTrailingTrivia(ifStatement.GetTrailingTrivia());
            }

            SyntaxList<StatementSyntax> newStatements = statementsInfo.Statements
                .Remove(ifStatement)
                .ReplaceAt(statementIndex, newStatement);

            return document.ReplaceStatementsAsync(statementsInfo, newStatements, cancellationToken);
        }
    }
}
