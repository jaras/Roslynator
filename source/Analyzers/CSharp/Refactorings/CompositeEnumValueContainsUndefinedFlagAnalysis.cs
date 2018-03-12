﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Refactorings
{
    //XPERF:
    internal static class CompositeEnumValueContainsUndefinedFlagAnalysis
    {
        public static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol flagsAttribute)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;

            if (namedType.TypeKind != TypeKind.Enum)
                return;

            if (!namedType.HasAttribute(flagsAttribute))
                return;

            ImmutableArray<ISymbol> fields = namedType.GetMembers();

            switch (namedType.EnumUnderlyingType.SpecialType)
            {
                case SpecialType.System_SByte:
                    {
                        ImmutableArray<sbyte> values = GetValues<sbyte>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (sbyte value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                case SpecialType.System_Byte:
                    {
                        ImmutableArray<byte> values = GetValues<byte>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (byte value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                case SpecialType.System_Int16:
                    {
                        ImmutableArray<short> values = GetValues<short>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (short value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                case SpecialType.System_UInt16:
                    {
                        ImmutableArray<ushort> values = GetValues<ushort>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (ushort value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                case SpecialType.System_Int32:
                    {
                        ImmutableArray<int> values = GetValues<int>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (int value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                case SpecialType.System_UInt32:
                    {
                        ImmutableArray<uint> values = GetValues<uint>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (uint value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                case SpecialType.System_Int64:
                    {
                        ImmutableArray<long> values = GetValues<long>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (long value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                case SpecialType.System_UInt64:
                    {
                        ImmutableArray<ulong> values = GetValues<ulong>(fields);

                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] != 0
                                && FlagsUtility.IsComposite(values[i]))
                            {
                                foreach (ulong value in FlagsUtility.Decompose(values[i]))
                                {
                                    if (values.IndexOf(value) == -1)
                                        ReportDiagnostic(context, fields[i], value.ToString());
                                }
                            }
                        }

                        break;
                    }
                default:
                    {
                        Debug.Fail(namedType.EnumUnderlyingType.SpecialType.ToString());
                        break;
                    }
            }
        }

        private static ImmutableArray<T> GetValues<T>(ImmutableArray<ISymbol> members)
        {
            return ImmutableArray.CreateRange(members, member =>
            {
                if (!member.IsImplicitlyDeclared
                    && member is IFieldSymbol fieldSymbol
                    && fieldSymbol.HasConstantValue)
                {
                    return (T)fieldSymbol.ConstantValue;
                }
                else
                {
                    return default(T);
                }
            });
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, ISymbol fieldSymbol, string value)
        {
            var enumMember = (EnumMemberDeclarationSyntax)fieldSymbol.GetSyntax(context.CancellationToken);

            context.ReportDiagnostic(
                DiagnosticDescriptors.CompositeEnumValueContainsUndefinedFlag,
                enumMember.GetLocation(),
                ImmutableDictionary.CreateRange(new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Value", value) }),
                value);
        }
    }
}
