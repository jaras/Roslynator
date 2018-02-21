﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp
{
    /// <summary>
    /// Represents selected statement(s) in a <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    public class StatementsSelection : SyntaxListSelection<StatementSyntax>
    {
        private StatementsSelection(SyntaxList<StatementSyntax> statements, TextSpan span, SelectionResult result)
             : this(statements, span, result.FirstIndex, result.LastIndex)
        {
        }

        private StatementsSelection(SyntaxList<StatementSyntax> statements, TextSpan span, int firstIndex, int lastIndex)
             : base(statements, span, firstIndex, lastIndex)
        {
        }

        /// <summary>
        /// Creates a new <see cref="StatementsSelection"/> based on the specified block and span.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="span"></param>
        /// <returns></returns>
        public static StatementsSelection Create(BlockSyntax block, TextSpan span)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            return CreateImpl(block.Statements, span);
        }

        /// <summary>
        /// Creates a new <see cref="StatementsSelection"/> based on the specified switch section and span.
        /// </summary>
        /// <param name="switchSection"></param>
        /// <param name="span"></param>
        /// <returns></returns>
        public static StatementsSelection Create(SwitchSectionSyntax switchSection, TextSpan span)
        {
            if (switchSection == null)
                throw new ArgumentNullException(nameof(switchSection));

            return CreateImpl(switchSection.Statements, span);
        }

        /// <summary>
        /// Creates a new <see cref="StatementsSelection"/> based on the specified <see cref="StatementsInfo"/> and span.
        /// </summary>
        /// <param name="statementsInfo"></param>
        /// <param name="span"></param>
        /// <returns></returns>
        public static StatementsSelection Create(StatementsInfo statementsInfo, TextSpan span)
        {
            return CreateImpl(statementsInfo.Statements, span);
        }

        private static StatementsSelection CreateImpl(SyntaxList<StatementSyntax> statements, TextSpan span)
        {
            SelectionResult result = SelectionResult.Create(statements, span);

            if (!result.Success)
                throw new InvalidOperationException("No selected statement(s) found.");

            return new StatementsSelection(statements, span, result);
        }

        /// <summary>
        /// Creates a new <see cref="StatementsSelection"/> based on the specified block and span.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="span"></param>
        /// <param name="selectedStatements"></param>
        /// <returns>True if the specified span contains at least one statement; otherwise, false.</returns>
        public static bool TryCreate(BlockSyntax block, TextSpan span, out StatementsSelection selectedStatements)
        {
            selectedStatements = Create(block, span, 1, int.MaxValue);
            return selectedStatements != null;
        }

        internal static bool TryCreate(BlockSyntax block, TextSpan span, int minCount, out StatementsSelection selectedStatements)
        {
            selectedStatements = Create(block, span, minCount, int.MaxValue);
            return selectedStatements != null;
        }

        internal static bool TryCreate(BlockSyntax block, TextSpan span, int minCount, int maxCount, out StatementsSelection selectedStatements)
        {
            selectedStatements = Create(block, span, minCount, maxCount);
            return selectedStatements != null;
        }

        private static StatementsSelection Create(BlockSyntax block, TextSpan span, int minCount, int maxCount)
        {
            if (block == null)
                return null;

            return Create(block.Statements, span, minCount, maxCount);
        }

        /// <summary>
        /// Creates a new <see cref="StatementsSelection"/> based on the specified switch section and span.
        /// </summary>
        /// <param name="switchSection"></param>
        /// <param name="span"></param>
        /// <param name="selectedStatements"></param>
        /// <returns>True if the specified span contains at least one statement; otherwise, false.</returns>
        public static bool TryCreate(SwitchSectionSyntax switchSection, TextSpan span, out StatementsSelection selectedStatements)
        {
            selectedStatements = Create(switchSection, span, 1, int.MaxValue);
            return selectedStatements != null;
        }

        internal static bool TryCreate(SwitchSectionSyntax switchSection, TextSpan span, int minCount, out StatementsSelection selectedStatements)
        {
            selectedStatements = Create(switchSection, span, minCount, int.MaxValue);
            return selectedStatements != null;
        }

        internal static bool TryCreate(SwitchSectionSyntax switchSection, TextSpan span, int minCount, int maxCount, out StatementsSelection selectedStatements)
        {
            selectedStatements = Create(switchSection, span, minCount, maxCount);
            return selectedStatements != null;
        }

        private static StatementsSelection Create(SwitchSectionSyntax switchSection, TextSpan span, int minCount, int maxCount)
        {
            if (switchSection == null)
                return null;

            return Create(switchSection.Statements, span, minCount, maxCount);
        }

        private static StatementsSelection Create(SyntaxList<StatementSyntax> statements, TextSpan span, int minCount, int maxCount)
        {
            SelectionResult result = SelectionResult.Create(statements, span, minCount, maxCount);

            if (!result.Success)
                return null;

            return new StatementsSelection(statements, span, result);
        }
    }
}
