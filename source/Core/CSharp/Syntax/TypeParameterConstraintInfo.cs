﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Roslynator.CSharp.Syntax.SyntaxInfoHelpers;

namespace Roslynator.CSharp.Syntax
{
    public struct TypeParameterConstraintInfo : IEquatable<TypeParameterConstraintInfo>
    {
        private static TypeParameterConstraintInfo Default { get; } = new TypeParameterConstraintInfo();

        private TypeParameterConstraintInfo(
            TypeParameterConstraintSyntax constraint,
            TypeParameterConstraintClauseSyntax constraintClause,
            SyntaxNode declaration,
            TypeParameterListSyntax typeParameterList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses)
        {
            Constraint = constraint;
            ConstraintClause = constraintClause;
            Declaration = declaration;
            TypeParameterList = typeParameterList;
            ConstraintClauses = constraintClauses;
        }

        public TypeParameterConstraintSyntax Constraint { get; }

        public TypeParameterConstraintClauseSyntax ConstraintClause { get; }

        public SeparatedSyntaxList<TypeParameterConstraintSyntax> Constraints
        {
            get { return ConstraintClause?.Constraints ?? default(SeparatedSyntaxList<TypeParameterConstraintSyntax>); }
        }

        public IdentifierNameSyntax Name
        {
            get { return ConstraintClause?.Name; }
        }

        public string NameText
        {
            get { return Name?.Identifier.ValueText; }
        }

        public SyntaxNode Declaration { get; }

        public TypeParameterListSyntax TypeParameterList { get; }

        public SeparatedSyntaxList<TypeParameterSyntax> TypeParameters
        {
            get { return TypeParameterList?.Parameters ?? default(SeparatedSyntaxList<TypeParameterSyntax>); }
        }

        public SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }

        public TypeParameterSyntax TypeParameter
        {
            get
            {
                foreach (TypeParameterSyntax typeParameter in TypeParameters)
                {
                    if (string.Equals(NameText, typeParameter.Identifier.ValueText, StringComparison.Ordinal))
                        return typeParameter;
                }

                return null;
            }
        }

        public bool Success
        {
            get { return Constraint != null; }
        }

        internal bool IsDuplicateConstraint
        {
            get
            {
                if (Constraint == null)
                    return false;

                SeparatedSyntaxList<TypeParameterConstraintSyntax> constraints = Constraints;

                int index = constraints.IndexOf(Constraint);

                SyntaxKind kind = Constraint.Kind();

                switch (kind)
                {
                    case SyntaxKind.ClassConstraint:
                    case SyntaxKind.StructConstraint:
                        {
                            for (int i = 0; i < index; i++)
                            {
                                if (constraints[i].Kind() == kind)
                                    return true;
                            }

                            break;
                        }
                }

                return false;
            }
        }

        internal static TypeParameterConstraintInfo Create(
            TypeParameterConstraintSyntax constraint,
            bool allowMissing = false)
        {
            if (!(constraint?.Parent is TypeParameterConstraintClauseSyntax constraintClause))
                return Default;

            IdentifierNameSyntax name = constraintClause.Name;

            if (!Check(name, allowMissing))
                return Default;

            SyntaxNode parent = constraintClause.Parent;

            switch (parent?.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    {
                        var classDeclaration = (ClassDeclarationSyntax)parent;

                        TypeParameterListSyntax typeParameterList = classDeclaration.TypeParameterList;

                        if (!Check(typeParameterList, allowMissing))
                            return Default;

                        return new TypeParameterConstraintInfo(constraint, constraintClause, classDeclaration, typeParameterList, classDeclaration.ConstraintClauses);
                    }
                case SyntaxKind.DelegateDeclaration:
                    {
                        var delegateDeclaration = (DelegateDeclarationSyntax)parent;

                        TypeParameterListSyntax typeParameterList = delegateDeclaration.TypeParameterList;

                        if (!Check(typeParameterList, allowMissing))
                            return Default;

                        return new TypeParameterConstraintInfo(constraint, constraintClause, delegateDeclaration, typeParameterList, delegateDeclaration.ConstraintClauses);
                    }
                case SyntaxKind.InterfaceDeclaration:
                    {
                        var interfaceDeclaration = (InterfaceDeclarationSyntax)parent;

                        TypeParameterListSyntax typeParameterList = interfaceDeclaration.TypeParameterList;

                        if (!Check(typeParameterList, allowMissing))
                            return Default;

                        return new TypeParameterConstraintInfo(constraint, constraintClause, interfaceDeclaration, interfaceDeclaration.TypeParameterList, interfaceDeclaration.ConstraintClauses);
                    }
                case SyntaxKind.LocalFunctionStatement:
                    {
                        var localFunctionStatement = (LocalFunctionStatementSyntax)parent;

                        TypeParameterListSyntax typeParameterList = localFunctionStatement.TypeParameterList;

                        if (!Check(typeParameterList, allowMissing))
                            return Default;

                        return new TypeParameterConstraintInfo(constraint, constraintClause, localFunctionStatement, typeParameterList, localFunctionStatement.ConstraintClauses);
                    }
                case SyntaxKind.MethodDeclaration:
                    {
                        var methodDeclaration = (MethodDeclarationSyntax)parent;

                        TypeParameterListSyntax typeParameterList = methodDeclaration.TypeParameterList;

                        if (!Check(typeParameterList, allowMissing))
                            return Default;

                        return new TypeParameterConstraintInfo(constraint, constraintClause, methodDeclaration, typeParameterList, methodDeclaration.ConstraintClauses);
                    }
                case SyntaxKind.StructDeclaration:
                    {
                        var structDeclaration = (StructDeclarationSyntax)parent;

                        TypeParameterListSyntax typeParameterList = structDeclaration.TypeParameterList;

                        if (!Check(typeParameterList, allowMissing))
                            return Default;

                        return new TypeParameterConstraintInfo(constraint, constraintClause, structDeclaration, typeParameterList, structDeclaration.ConstraintClauses);
                    }
            }

            return Default;
        }

        public override string ToString()
        {
            return Constraint?.ToString() ?? base.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is TypeParameterConstraintInfo other && Equals(other);
        }

        public bool Equals(TypeParameterConstraintInfo other)
        {
            return EqualityComparer<SyntaxNode>.Default.Equals(Declaration, other.Declaration);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<SyntaxNode>.Default.GetHashCode(Declaration);
        }

        public static bool operator ==(TypeParameterConstraintInfo info1, TypeParameterConstraintInfo info2)
        {
            return info1.Equals(info2);
        }

        public static bool operator !=(TypeParameterConstraintInfo info1, TypeParameterConstraintInfo info2)
        {
            return !(info1 == info2);
        }
    }
}