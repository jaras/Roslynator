﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class DeclareEachAttributeSeparatelyAnalysis
    {
        public static bool CanRefactor(AttributeListSyntax attributeList)
        {
            return attributeList.Attributes.Count > 1;
        }
    }
}
