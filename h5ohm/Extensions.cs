/* Copyright (c) 2020 lefi7z
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;


namespace H5Ohm.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool IsNull<T>(this IEnumerable<T> instance)
        {
            return instance == null;
        }

        public static bool IsEmpty<T>(this IEnumerable<T> instance)
        {
            if(instance.IsNull())
                throw new NullReferenceException();
            return instance.Count() == 0;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> instance)
        {
            return instance.IsNull() || instance.IsEmpty(); 
        }

        public static bool HasSameLengthAs<T, U>(this IEnumerable<T> instance, IEnumerable<U> array)
        {
            if (instance.IsNull())
                throw new NullReferenceException();
            if(array.IsNull())
                throw new ArgumentNullException();
            return instance.Count() == array.Count();
        }

        public static List<string> ConvertToStringList(
            this IEnumerable<double> instance, string format = "", IFormatProvider formatProvider = null)
        {
            if (instance.IsNull())
                throw new NullReferenceException();
            List<string> converted = new List<string>();
            var enumerator = instance.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string toStringResult = (format == "" && formatProvider == null)?
                    enumerator.Current.ToString() : 
                    enumerator.Current.ToString(format, formatProvider);
                converted.Add(toStringResult);
            }
            return converted;
        }

        /// <summary>
        /// Applies a function to each element of the sequence.
        /// </summary>
        public static void Apply<T>(this IEnumerable<T> instance, Action<T> action)
        {
            foreach (T elm in instance)
                action(elm);
        }

        /// <summary>
        /// Returns the last element of a sequence. Equivalent to `IEnumerable.Last()`, but can be magnitudes faster.
        /// </summary>
        public static T Final<T>(this IEnumerable<T> instance)
        {
            return instance.Skip(instance.Count() - 1).First();
        }

        /// <summary>
        /// Returns only those elements of a sequence for which the predicate is true.
        /// </summary>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> instance, IEnumerable<bool> predicate)
        {
            var value = instance.GetEnumerator();
            var pred = predicate.GetEnumerator();
            while (value.MoveNext() && pred.MoveNext())
                if (pred.Current)
                    yield return value.Current;
        }
    }

    public static class ArrayExtensions
    {
        public static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = value;
        }

        public static double[] ToDouble(this float[] instance)
        {
            return Array.ConvertAll(instance, x => (double)x);
        }
    }
}
