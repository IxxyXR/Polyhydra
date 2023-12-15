﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Drawing;
using UnityEngine;
using zCode.zData;
using Color = UnityEngine.Color;

/*
 * Notes
 *
 * IReadonlyList extension methods are redirected to equivalent array extension methods where possible for better performance.
 */

namespace zCode.zCore
{
    /// <summary>
    ///
    /// </summary>
    public static partial class IReadOnlyListExtension
    {
        #region IReadOnlyList<T>

        /// <summary>
        ///
        /// </summary>
        public static T[] GetRange<T>(this IReadOnlyList<T> list, int index, int count)
        {
            if (list is T[] arr)
                return ArrayExtension.GetRange(arr, index, count);

            T[] result = new T[count];
            GetRangeImpl(list, index, count, result);
            return result;
        }


        /// <summary>
        ///
        /// </summary>
        public static void GetRange<T>(this IReadOnlyList<T> list, int index, int count, IList<T> result)
        {
            if (list is T[] arr0 && result is T[] arr1)
                ArrayExtension.GetRange(arr0, index, count, arr1);
            else
                GetRangeImpl(list, index, count, result);
        }


        /// <summary>
        ///
        /// </summary>
        private static void GetRangeImpl<T>(IReadOnlyList<T> list, int index, int count, IList<T> result)
        {
            for (int i = 0; i < count; i++)
                result[i] = list[i + index];
        }


        /// <summary>
        ///
        /// </summary>
        public static T[] GetSelection<T>(this IReadOnlyList<T> list, IReadOnlyList<int> indices)
        {
            if (list is T[] arr0 && indices is int[] arr1)
                return ArrayExtension.GetSelection(arr0, arr1);

            T[] result = new T[indices.Count];
            GetSelectionImpl(list, indices, result);
            return result;
        }


        /// <summary>
        ///
        /// </summary>
        public static void GetSelection<T>(this IReadOnlyList<T> list, IReadOnlyList<int> indices, IList<T> result)
        {
            if (list is T[] arr0 && indices is int[] arr1 && result is T[] arr2)
                ArrayExtension.GetSelection(arr0, arr1, arr2);
            else
                GetSelectionImpl(list, indices, result);
        }


        /// <summary>
        ///
        /// </summary>
        private static void GetSelectionImpl<T>(IReadOnlyList<T> list, IReadOnlyList<int> indices, IList<T> result)
        {
            for (int i = 0; i < indices.Count; i++)
                result[i] = list[indices[i]];
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static void Action<T>(this IReadOnlyList<T> source, Action<T> action, bool parallel = false)
        {
            ActionRange(source, 0, source.Count, action, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static void Action<T>(this IReadOnlyList<T> source, Action<T, int> action, bool parallel = false)
        {
            ActionRange(source, 0, source.Count, action, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        public static U[] Convert<T, U>(this IReadOnlyList<T> source, Func<T, U> converter, bool parallel = false)
        {
            return ConvertRange(source, 0, source.Count, converter, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        public static void Convert<T, U>(this IReadOnlyList<T> source, Func<T, U> converter, IList<U> result, bool parallel = false)
        {
            ConvertRange(source, 0, source.Count, converter, result, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        public static U[] Convert<T, U>(this IReadOnlyList<T> source, Func<T, int, U> converter, bool parallel = false)
        {
            return ConvertRange(source, 0, source.Count, converter, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        public static void Convert<T, U>(this IReadOnlyList<T> source, Func<T, int, U> converter, IList<U> result, bool parallel = false)
        {
            ConvertRange(source, 0, source.Count, converter, result, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <param name="action"></param>
        public static void ActionRange<T>(this IReadOnlyList<T> source, int index, int count, Action<T> action, bool parallel = false)
        {
            if (source is T[] arr)
            {
                ArrayExtension.ActionRange(arr, index, count, action, parallel);
                return;
            }

            if (parallel)
            {
                Parallel.ForEach(Partitioner.Create(0, count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        action(source[i + index]);
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                    action(source[i + index]);
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <param name="action"></param>
        public static void ActionRange<T>(this IReadOnlyList<T> source, int index, int count, Action<T, int> action, bool parallel = false)
        {
            if (source is T[] arr)
            {
                ArrayExtension.ActionRange(arr, index, count, action, parallel);
                return;
            }

            if (parallel)
            {
                Parallel.ForEach(Partitioner.Create(0, count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        int j = i + index;
                        action(source[j], j);
                    }
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int j = i + index;
                    action(source[j], j);
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        public static U[] ConvertRange<T, U>(this IReadOnlyList<T> source, int index, int count, Func<T, U> converter, bool parallel = false)
        {
            if (source is T[] arr)
                return ArrayExtension.ConvertRange(arr, index, count, converter, parallel);

            U[] result = new U[count];
            ConvertRangeImpl(source, index, count, converter, result, parallel);
            return result;
        }


        /// <summary>
        ///
        /// </summary>
        public static void ConvertRange<T, U>(this IReadOnlyList<T> source, int index, int count, Func<T, U> converter, IList<U> result, bool parallel = false)
        {
            if (source is T[] arr0 && result is U[] arr1)
                ArrayExtension.ConvertRange(arr0, index, count, converter, arr1, parallel);
            else
                ConvertRangeImpl(source, index, count, converter, result, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        private static void ConvertRangeImpl<T,U>(IReadOnlyList<T> source, int index, int count, Func<T, U> converter, IList<U> result, bool parallel = false)
        {
            if(parallel)
            {
                Parallel.ForEach(Partitioner.Create(0, count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        result[i] = converter(source[i + index]);
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                    result[i] = converter(source[i + index]);
            }
        }


        /// <summary>
        ///
        /// </summary>
        public static U[] ConvertRange<T, U>(this IReadOnlyList<T> source, int index, int count, Func<T, int, U> converter, bool parallel = false)
        {
            if (source is T[] arr)
                return ArrayExtension.ConvertRange(arr, index, count, converter, parallel);

            U[] result = new U[count];
            ConvertRangeImpl(source, index, count, converter, result, parallel);
            return result;
        }


        /// <summary>
        ///
        /// </summary>
        public static void ConvertRange<T, U>(this IReadOnlyList<T> source, int index, int count, Func<T, int, U> converter, IList<U> result, bool parallel = false)
        {
            if (source is T[] arr0 && result is U[] arr1)
                ArrayExtension.ConvertRange(arr0, index, count, converter, arr1, parallel);
            else
                ConvertRangeImpl(source, index, count, converter, result, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        private static void ConvertRangeImpl<T, U>(IReadOnlyList<T> source, int index, int count, Func<T, int, U> converter, IList<U> result, bool parallel = false)
        {
            if(parallel)
            {
                Parallel.ForEach(Partitioner.Create(0, count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        int j = i + index;
                        result[i] = converter(source[j], j);
                    }
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int j = i + index;
                    result[i] = converter(source[j], j);
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        public static void ActionSelection<T>(IReadOnlyList<T> source, IReadOnlyList<int> indices, Action<T> action, bool parallel = false)
        {
            if (source is T[] arr0 && indices is int[] arr1)
            {
                ArrayExtension.ActionSelection<T>(arr0, arr1, action, parallel);
                return;
            }

            if (parallel)
            {
                Parallel.ForEach(Partitioner.Create(0, indices.Count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        action(source[indices[i]]);
                });
            }
            else
            {
                for (int i = 0; i < indices.Count; i++)
                    action(source[indices[i]]);
            }
        }


        /// <summary>
        ///
        /// </summary>
        public static U[] ConvertSelection<T, U>(this IReadOnlyList<T> source, IReadOnlyList<int> indices, Func<T, U> converter, bool parallel = false)
        {
            if (source is T[] arr0 && indices is int[] arr1)
                return ArrayExtension.ConvertSelection(arr0, arr1, converter, parallel);

            U[] result = new U[indices.Count];
            ConvertSelectionImpl(source, indices, converter, result, parallel);
            return result;
        }


        /// <summary>
        ///
        /// </summary>
        public static void ConvertSelection<T, U>(this IReadOnlyList<T> source, IReadOnlyList<int> indices, Func<T, U> converter, IList<U> result, bool parallel = false)
        {
            if (source is T[] arr0 && indices is int[] arr1 && result is U[] arr2)
                ArrayExtension.ConvertSelection(arr0, arr1, converter, arr2, parallel);
            else
                ConvertSelectionImpl(source, indices, converter, result, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        private static void ConvertSelectionImpl<T, U>(IReadOnlyList<T> source, IReadOnlyList<int> indices, Func<T, U> converter, IList<U> result, bool parallel = false)
        {
            if (parallel)
            {
                Parallel.ForEach(Partitioner.Create(0, indices.Count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        result[i] = converter(source[indices[i]]);
                });
            }
            else
            {
                for (int i = 0; i < indices.Count; i++)
                    result[i] = converter(source[indices[i]]);
            }
        }


        /// <summary>
        ///
        /// </summary>
        public static U[] ConvertSelection<T, U>(this IReadOnlyList<T> source, IReadOnlyList<int> indices, Func<T, int, U> converter, bool parallel = false)
        {
            if (source is T[] arr0 && indices is int[] arr1)
                return ArrayExtension.ConvertSelection(arr0, arr1, converter, parallel);

            U[] result = new U[indices.Count];
            ConvertSelectionImpl(source, indices, converter, result, parallel);
            return result;
        }


        /// <summary>
        ///
        /// </summary>
        public static void ConvertSelection<T, U>(this IReadOnlyList<T> source, IReadOnlyList<int> indices, Func<T, int, U> converter, IList<U> result, bool parallel = false)
        {
            if (source is T[] arr0 && indices is int[] arr1 && result is U[] arr2)
                ArrayExtension.ConvertSelection(arr0, arr1, converter, arr2, parallel);
            else
                ConvertSelectionImpl(source, indices, converter, result, parallel);
        }


        /// <summary>
        ///
        /// </summary>
        private static void ConvertSelectionImpl<T, U>(IReadOnlyList<T> source, IReadOnlyList<int> indices, Func<T, int, U> converter, IList<U> result, bool parallel = false)
        {
            if (parallel)
            {
                Parallel.ForEach(Partitioner.Create(0, indices.Count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        int j = indices[i];
                        result[i] = converter(source[j], j);
                    }
                });
            }
            else
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int j = indices[i];
                    result[i] = converter(source[j], j);
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        public static IEnumerable<T> TakeRange<T>(this IReadOnlyList<T> source, int index, int count)
        {
            for (int i = 0; i < count; i++)
                yield return source[i + index];
        }


        /// <summary>
        ///
        /// </summary>
        public static IEnumerable<T> TakeSelection<T>(this IReadOnlyList<T> source, IEnumerable<int> indices)
        {
            foreach (int i in indices)
                yield return source[i];
        }


        /// <summary>
        ///
        /// </summary>
        public static IEnumerable<T> TakeEveryNth<T>(this IReadOnlyList<T> source, int n)
        {
            return TakeEveryNth(source, n, 0, source.Count);
        }


        /// <summary>
        ///
        /// </summary>
        public static IEnumerable<T> TakeEveryNth<T>(this IReadOnlyList<T> source, int n, int index, int count)
        {
            for (int i = 0; i < count; i += n)
                yield return source[i + index];
        }

        #endregion


        #region IReadOnlyList<Color>

        /// <summary>
        ///
        /// </summary>
        public static Color Lerp(this IReadOnlyList<Color> colors, double factor)
        {
            int last = colors.Count - 1;
            factor = zMath.Fract(factor * last, out int i);

            if (i < 0)
                return colors[0];
            else if (i >= last)
                return colors[last];

            return colors[i].LerpTo(colors[i + 1], factor);
        }

        #endregion


        #region IReadOnlyList<double>

        /// <summary>
        ///
        /// </summary>
        public static double Lerp(this IReadOnlyList<double> vector, double factor)
        {
            int last = vector.Count - 1;
            factor = zMath.Fract(factor * last, out int i);

            if (i < 0)
                return vector[0];
            else if (i >= last)
                return vector[last];

            return zMath.Lerp(vector[i], vector[i + 1], factor);
        }

        #endregion


        #region IReadOnlyList<Vec2d>

        /// <summary>
        ///
        /// </summary>
        public static Vec2d Lerp(this IReadOnlyList<Vec2d> vectors, double factor)
        {
            int last = vectors.Count - 1;
            factor = zMath.Fract(factor * last, out int i);

            if (i < 0)
                return vectors[0];
            else if (i >= last)
                return vectors[last];

            return vectors[i].LerpTo(vectors[i + 1], factor);
        }

        #endregion


        #region IReadOnlyList<Vec3d>

        /// <summary>
        ///
        /// </summary>
        public static Vec3d Lerp(this IReadOnlyList<Vec3d> vectors, double factor)
        {
            int last = vectors.Count - 1;
            factor = zMath.Fract(factor * last, out int i);

            if (i < 0)
                return vectors[0];
            else if (i >= last)
                return vectors[last];

            return vectors[i].LerpTo(vectors[i + 1], factor);
        }

        #endregion


        #region IReadOnlyList<double[]>

        /// <summary>
        ///
        /// </summary>
        public static void Lerp(this IReadOnlyList<double[]> vectors, double factor, double[] result)
        {
            Lerp(vectors, factor, vectors[0].Length, result);
        }


        /// <summary>
        ///
        /// </summary>
        public static void Lerp(this IReadOnlyList<double[]> vectors, double factor, int size, double[] result)
        {
            int last = vectors.Count - 1;
            factor = zMath.Fract(factor * last, out int i);

            if (i < 0)
                result.Set(vectors[0]);
            else if (i >= last)
                result.Set(vectors[last]);
            else
                ArrayMath.Lerp(vectors[i], vectors[i + 1], factor, size, result);
        }

        #endregion
    }
}
