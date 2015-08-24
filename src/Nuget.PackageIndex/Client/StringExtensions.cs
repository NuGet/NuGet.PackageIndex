// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// string extensions helper methods
    /// </summary>
    internal static class StringExtensions
    {
        public static string NormalizeGenericName(this string self)
        {
            var temp = self;
            var bracketIndex = temp.IndexOf("<");
            if (bracketIndex >= 0)
            {
                temp = temp.Substring(0, bracketIndex);
            }

            temp = temp.Trim('\r', '\n', ' ');
            return temp;
        }
    }
}
