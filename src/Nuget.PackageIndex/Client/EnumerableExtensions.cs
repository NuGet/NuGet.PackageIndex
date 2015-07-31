// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    public static class EnumerableExtensions
    {
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return OfTypeIterator<TResult>(source);
        }

        static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source)
            {
                if (obj is TResult) yield return (TResult)obj;
            }
        }

        public static bool IsSorted<T>(this IEnumerable<T> enumerable, IComparer<T> comparer)
        {
            using (var e = enumerable.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    return true;
                }

                var previous = e.Current;
                while (e.MoveNext())
                {
                    if (comparer.Compare(previous, e.Current) > 0)
                    {
                        return false;
                    }

                    previous = e.Current;
                }

                return true;
            }
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return source.ConcatWorker(value);
        }

        private static IEnumerable<T> ConcatWorker<T>(this IEnumerable<T> source, T value)
        {
            foreach (var v in source)
            {
                yield return v;
            }

            yield return value;
        }
    }
}
