﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslynator.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Syntax
{
    internal readonly struct StringConcatenationExpressionInfo : IEquatable<StringConcatenationExpressionInfo>, IReadOnlyList<ExpressionSyntax>
    {
        private StringConcatenationExpressionInfo(
            BinaryExpressionSyntax addExpression,
            ImmutableArray<ExpressionSyntax> expressions,
            TextSpan? span = null)
        {
            ContainsNonSpecificExpression = false;
            ContainsRegularLiteralExpression = false;
            ContainsVerbatimLiteralExpression = false;
            ContainsRegularInterpolatedStringExpression = false;
            ContainsVerbatimInterpolatedStringExpression = false;

            OriginalExpression = addExpression;
            Expressions = expressions;
            Span = span;

            foreach (ExpressionSyntax expression in expressions)
            {
                SyntaxKind kind = expression.Kind();

                if (kind == SyntaxKind.StringLiteralExpression)
                {
                    if (((LiteralExpressionSyntax)expression).IsVerbatimStringLiteral())
                    {
                        ContainsVerbatimLiteralExpression = true;
                    }
                    else
                    {
                        ContainsRegularLiteralExpression = true;
                    }
                }
                else if (kind == SyntaxKind.InterpolatedStringExpression)
                {
                    if (((InterpolatedStringExpressionSyntax)expression).IsVerbatim())
                    {
                        ContainsVerbatimInterpolatedStringExpression = true;
                    }
                    else
                    {
                        ContainsRegularInterpolatedStringExpression = true;
                    }
                }
                else
                {
                    ContainsNonSpecificExpression = true;
                }
            }
        }

        private static StringConcatenationExpressionInfo Default { get; } = new StringConcatenationExpressionInfo();

        public ImmutableArray<ExpressionSyntax> Expressions { get; }

        public BinaryExpressionSyntax OriginalExpression { get; }

        public TextSpan? Span { get; }

        public int Count
        {
            get { return Expressions.Length; }
        }

        public ExpressionSyntax this[int index]
        {
            get { return Expressions[index]; }
        }

        IEnumerator<ExpressionSyntax> IEnumerable<ExpressionSyntax>.GetEnumerator()
        {
            return ((IEnumerable<ExpressionSyntax>)Expressions).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Expressions).GetEnumerator();
        }

        public ImmutableArray<ExpressionSyntax>.Enumerator GetEnumerator()
        {
            return Expressions.GetEnumerator();
        }

        public bool ContainsNonSpecificExpression { get; }

        public bool ContainsNonLiteralExpression
        {
            get { return ContainsInterpolatedStringExpression || ContainsNonSpecificExpression; }
        }

        public bool ContainsLiteralExpression
        {
            get { return ContainsRegularLiteralExpression || ContainsVerbatimLiteralExpression; }
        }

        public bool ContainsRegularLiteralExpression { get; }

        public bool ContainsVerbatimLiteralExpression { get; }

        public bool ContainsInterpolatedStringExpression
        {
            get { return ContainsRegularInterpolatedStringExpression || ContainsVerbatimInterpolatedStringExpression; }
        }

        public bool ContainsRegularInterpolatedStringExpression { get; }

        public bool ContainsVerbatimInterpolatedStringExpression { get; }

        public bool ContainsRegular
        {
            get { return ContainsRegularLiteralExpression || ContainsRegularInterpolatedStringExpression; }
        }

        public bool ContainsVerbatim
        {
            get { return ContainsVerbatimLiteralExpression || ContainsVerbatimInterpolatedStringExpression; }
        }

        public bool Success
        {
            get { return OriginalExpression != null; }
        }

        internal static StringConcatenationExpressionInfo Create(
            BinaryExpressionSyntax binaryExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (binaryExpression?.Kind() != SyntaxKind.AddExpression)
                return Default;

            if (semanticModel == null)
                throw new ArgumentNullException(nameof(semanticModel));

            ImmutableArray<ExpressionSyntax> expressions = GetExpressions(binaryExpression, semanticModel, cancellationToken);

            if (expressions.IsDefault)
                return Default;

            return new StringConcatenationExpressionInfo(binaryExpression, expressions);
        }

        internal static StringConcatenationExpressionInfo Create(
            BinaryExpressionSelection binaryExpressionSelection,
            SemanticModel semanticModel,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BinaryExpressionSyntax binaryExpression = binaryExpressionSelection.BinaryExpression;

            if (binaryExpression?.Kind() != SyntaxKind.AddExpression)
                return Default;

            if (semanticModel == null)
                throw new ArgumentNullException(nameof(semanticModel));

            ImmutableArray<ExpressionSyntax> expressions = binaryExpressionSelection.Expressions;

            foreach (ExpressionSyntax expression in expressions)
            {
                if (!IsStringExpression(expression, semanticModel, cancellationToken))
                    return Default;
            }

            return new StringConcatenationExpressionInfo(binaryExpression, expressions, binaryExpressionSelection.Span);
        }

        private static bool IsStringExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (expression == null)
                return false;

            if (expression.Kind().Is(
                SyntaxKind.StringLiteralExpression,
                SyntaxKind.InterpolatedStringExpression))
            {
                return true;
            }

            return semanticModel.GetTypeInfo(expression, cancellationToken)
                .ConvertedType?
                .IsString() == true;
        }

        private static ImmutableArray<ExpressionSyntax> GetExpressions(
            BinaryExpressionSyntax binaryExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            ImmutableArray<ExpressionSyntax>.Builder builder = null;

            while (true)
            {
                MethodInfo methodInfo = semanticModel.GetMethodInfo(binaryExpression, cancellationToken);

                if (methodInfo.Symbol != null
                    && methodInfo.MethodKind == MethodKind.BuiltinOperator
                    && methodInfo.Name == WellKnownMemberNames.AdditionOperatorName
                    && methodInfo.IsContainingType(SpecialType.System_String))
                {
                    (builder ?? (builder = ImmutableArray.CreateBuilder<ExpressionSyntax>())).Add(binaryExpression.Right);

                    ExpressionSyntax left = binaryExpression.Left;

                    if (left.IsKind(SyntaxKind.AddExpression))
                    {
                        binaryExpression = (BinaryExpressionSyntax)left;
                    }
                    else
                    {
                        builder.Add(left);
                        builder.Reverse();
                        return builder.ToImmutable();
                    }
                }
                else
                {
                    return default(ImmutableArray<ExpressionSyntax>);
                }
            }
        }

        public InterpolatedStringExpressionSyntax ToInterpolatedString()
        {
            ThrowInvalidOperationIfNotInitialized();

            StringBuilder sb = StringBuilderCache.GetInstance();

            sb.Append('$');

            if (!ContainsRegular)
                sb.Append('@');

            sb.Append('"');

            for (int i = 0; i < Expressions.Length; i++)
            {
                SyntaxKind kind = Expressions[i].Kind();

                if (kind == SyntaxKind.StringLiteralExpression)
                {
                    var literal = (LiteralExpressionSyntax)Expressions[i];

                    if (ContainsRegular
                        && literal.IsVerbatimStringLiteral())
                    {
                        string s = literal.Token.ValueText;
                        s = StringUtility.DoubleBackslash(s);
                        s = StringUtility.EscapeQuote(s);
                        s = StringUtility.DoubleBraces(s);
                        s = s.Replace("\n", @"\n");
                        s = s.Replace("\r", @"\r");
                        sb.Append(s);
                    }
                    else
                    {
                        string s = GetInnerText(literal.Token.Text);
                        s = StringUtility.DoubleBraces(s);
                        sb.Append(s);
                    }
                }
                else if (kind == SyntaxKind.InterpolatedStringExpression)
                {
                    var interpolatedString = (InterpolatedStringExpressionSyntax)Expressions[i];

                    bool isVerbatimInterpolatedString = interpolatedString.IsVerbatim();

                    foreach (InterpolatedStringContentSyntax content in interpolatedString.Contents)
                    {
                        Debug.Assert(content.IsKind(SyntaxKind.Interpolation, SyntaxKind.InterpolatedStringText), content.Kind().ToString());

                        switch (content.Kind())
                        {
                            case SyntaxKind.InterpolatedStringText:
                                {
                                    var text = (InterpolatedStringTextSyntax)content;

                                    if (ContainsRegular
                                        && isVerbatimInterpolatedString)
                                    {
                                        string s = text.TextToken.ValueText;
                                        s = StringUtility.DoubleBackslash(s);
                                        s = StringUtility.EscapeQuote(s);
                                        s = s.Replace("\n", @"\n");
                                        s = s.Replace("\r", @"\r");
                                        sb.Append(s);
                                    }
                                    else
                                    {
                                        sb.Append(content.ToString());
                                    }

                                    break;
                                }
                            case SyntaxKind.Interpolation:
                                {
                                    sb.Append(content.ToString());
                                    break;
                                }
                        }
                    }
                }
                else
                {
                    sb.Append('{');
                    sb.Append(Expressions[i].ToString());
                    sb.Append('}');
                }
            }

            sb.Append("\"");

            return (InterpolatedStringExpressionSyntax)ParseExpression(StringBuilderCache.GetStringAndFree(sb));
        }

        public LiteralExpressionSyntax ToStringLiteral()
        {
            ThrowInvalidOperationIfNotInitialized();

            if (ContainsNonLiteralExpression)
                throw new InvalidOperationException();

            StringBuilder sb = StringBuilderCache.GetInstance();

            if (!ContainsRegular)
                sb.Append('@');

            sb.Append('"');

            foreach (ExpressionSyntax expression in Expressions)
            {
                if (expression.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var literal = (LiteralExpressionSyntax)expression;

                    if (ContainsRegular
                        && literal.IsVerbatimStringLiteral())
                    {
                        string s = literal.Token.ValueText;
                        s = StringUtility.DoubleBackslash(s);
                        s = StringUtility.EscapeQuote(s);
                        s = s.Replace("\n", @"\n");
                        s = s.Replace("\r", @"\r");
                        sb.Append(s);
                    }
                    else
                    {
                        sb.Append(GetInnerText(literal.Token.Text));
                    }
                }
            }

            sb.Append('"');

            return (LiteralExpressionSyntax)ParseExpression(StringBuilderCache.GetStringAndFree(sb));
        }

        public LiteralExpressionSyntax ToMultilineStringLiteral()
        {
            ThrowInvalidOperationIfNotInitialized();

            if (ContainsNonLiteralExpression)
                throw new InvalidOperationException();

            StringBuilder sb = StringBuilderCache.GetInstance();

            sb.Append('@');
            sb.Append('"');

            for (int i = 0; i < Expressions.Length; i++)
            {
                if (Expressions[i].IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var literal = (LiteralExpressionSyntax)Expressions[i];

                    string s = StringUtility.DoubleQuote(literal.Token.ValueText);

                    int charCount = 0;

                    if (s.Length > 0
                        && s[s.Length - 1] == '\n')
                    {
                        charCount = 1;

                        if (s.Length > 1
                            && s[s.Length - 2] == '\r')
                        {
                            charCount = 2;
                        }
                    }

                    sb.Append(s, 0, s.Length - charCount);

                    if (charCount > 0)
                    {
                        sb.AppendLine();
                    }
                    else if (i < Expressions.Length - 1)
                    {
                        TextSpan span = TextSpan.FromBounds(Expressions[i].Span.End, Expressions[i + 1].SpanStart);

                        if (OriginalExpression.SyntaxTree.IsMultiLineSpan(span))
                            sb.AppendLine();
                    }
                }
            }

            sb.Append('"');

            return (LiteralExpressionSyntax)ParseExpression(StringBuilderCache.GetStringAndFree(sb));
        }

        public override string ToString()
        {
            return (Span != null)
                ? OriginalExpression.ToString(Span.Value)
                : OriginalExpression.ToString();
        }

        private static string GetInnerText(string s)
        {
            return (s[0] == '@')
                ? s.Substring(2, s.Length - 3)
                : s.Substring(1, s.Length - 2);
        }

        private void ThrowInvalidOperationIfNotInitialized()
        {
            if (OriginalExpression == null)
                throw new InvalidOperationException($"{nameof(StringConcatenationExpressionInfo)} is not initalized.");
        }

        public override bool Equals(object obj)
        {
            return obj is StringConcatenationExpressionInfo other && Equals(other);
        }

        public bool Equals(StringConcatenationExpressionInfo other)
        {
            return EqualityComparer<BinaryExpressionSyntax>.Default.Equals(OriginalExpression, other.OriginalExpression)
                && EqualityComparer<TextSpan?>.Default.Equals(Span, other.Span);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Span.GetHashCode(), Hash.Create(OriginalExpression));
        }

        public static bool operator ==(StringConcatenationExpressionInfo info1, StringConcatenationExpressionInfo info2)
        {
            return info1.Equals(info2);
        }

        public static bool operator !=(StringConcatenationExpressionInfo info1, StringConcatenationExpressionInfo info2)
        {
            return !(info1 == info2);
        }
    }
}
