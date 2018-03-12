﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Refactorings
{
    internal static class FormatDeclarationBracesAnalysis
    {
        public static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            if (!classDeclaration.Members.Any())
                Analyze(context, classDeclaration, classDeclaration.OpenBraceToken, classDeclaration.CloseBraceToken);
        }

        public static void AnalyzeStructDeclaration(SyntaxNodeAnalysisContext context)
        {
            var structDeclaration = (StructDeclarationSyntax)context.Node;

            if (!structDeclaration.Members.Any())
                Analyze(context, structDeclaration, structDeclaration.OpenBraceToken, structDeclaration.CloseBraceToken);
        }

        public static void AnalyzeInterfaceDeclaration(SyntaxNodeAnalysisContext context)
        {
            var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;

            if (!interfaceDeclaration.Members.Any())
                Analyze(context, interfaceDeclaration, interfaceDeclaration.OpenBraceToken, interfaceDeclaration.CloseBraceToken);
        }

        private static void Analyze(
            SyntaxNodeAnalysisContext context,
            MemberDeclarationSyntax declaration,
            SyntaxToken openBrace,
            SyntaxToken closeBrace)
        {
            if (openBrace.IsMissing)
                return;

            if (closeBrace.IsMissing)
                return;

            if (declaration.SyntaxTree.GetLineCount(TextSpan.FromBounds(openBrace.Span.End, closeBrace.SpanStart)) == 2)
                return;

            if (!openBrace.TrailingTrivia.IsEmptyOrWhitespace())
                return;

            if (!closeBrace.LeadingTrivia.IsEmptyOrWhitespace())
                return;

            context.ReportDiagnostic(DiagnosticDescriptors.FormatDeclarationBraces, openBrace);
        }
    }
}
