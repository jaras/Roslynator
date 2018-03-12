﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ImplementExceptionConstructorsAnalysis
    {
        public static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol exceptionSymbol)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;

            if (symbol.TypeKind != TypeKind.Class)
                return;

            if (symbol.IsStatic)
                return;

            if (symbol.IsImplicitClass)
                return;

            if (symbol.IsImplicitlyDeclared)
                return;

            INamedTypeSymbol baseType = symbol.BaseType;

            if (baseType?.IsObject() != false)
                return;

            if (!baseType.EqualsOrInheritsFrom(exceptionSymbol))
                return;

            if (!GenerateBaseConstructorsAnalysis.IsAnyBaseConstructorMissing(symbol, baseType))
                return;

            var classDeclaration = (ClassDeclarationSyntax)symbol.GetSyntax(context.CancellationToken);

            context.ReportDiagnostic(DiagnosticDescriptors.ImplementExceptionConstructors, classDeclaration.Identifier);
        }
    }
}
