﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;

namespace Renci.SshNet
{
    /// <summary>
    /// Collection of different extension method
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Checks whether a collection is the same as another collection
        /// </summary>
        /// <param name="value">The current instance object</param>
        /// <param name="compareList">The collection to compare with</param>
        /// <param name="comparer">The comparer object to use to compare each item in the collection.  If null uses EqualityComparer(T).Default</param>
        /// <returns>True if the two collections contain all the same items in the same order</returns>
        internal static bool IsEqualTo<TSource>(this IEnumerable<TSource> value, IEnumerable<TSource> compareList, IEqualityComparer<TSource> comparer)
        {
            if (value == compareList)
            {
                return true;
            }
            else if (value == null || compareList == null)
            {
                return false;
            }
            else
            {
                if (comparer == null)
                {
                    comparer = EqualityComparer<TSource>.Default;
                }

                IEnumerator<TSource> enumerator1 = value.GetEnumerator();
                IEnumerator<TSource> enumerator2 = compareList.GetEnumerator();

                bool enum1HasValue = enumerator1.MoveNext();
                bool enum2HasValue = enumerator2.MoveNext();

                try
                {
                    while (enum1HasValue && enum2HasValue)
                    {
                        if (!comparer.Equals(enumerator1.Current, enumerator2.Current))
                        {
                            return false;
                        }

                        enum1HasValue = enumerator1.MoveNext();
                        enum2HasValue = enumerator2.MoveNext();
                    }

                    return !(enum1HasValue || enum2HasValue);
                }
                finally
                {
                    if (enumerator1 != null) enumerator1.Dispose();
                    if (enumerator2 != null) enumerator2.Dispose();
                }
            }
        }

        /// <summary>
        /// Checks whether a collection is the same as another collection
        /// </summary>
        /// <param name="value">The current instance object</param>
        /// <param name="compareList">The collection to compare with</param>
        /// <returns>True if the two collections contain all the same items in the same order</returns>
        internal static bool IsEqualTo<TSource>(this IEnumerable<TSource> value, IEnumerable<TSource> compareList)
        {
            return IsEqualTo(value, compareList, null);
        }

#if SILVERLIGHT
#else

        /// <summary>
        /// Prints out 
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        internal static void DebugPrint(this IEnumerable<byte> bytes)
        {
            foreach (var b in bytes)
            {
                Debug.Write(string.Format(CultureInfo.CurrentCulture, "0x{0:x2}, ", b));
            }
            Debug.WriteLine(string.Empty);
        }
#endif

        /// <summary>
        /// Trims the leading zero from bytes array.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        internal static IEnumerable<byte> TrimLeadingZero(this IEnumerable<byte> data)
        {
            bool leadingZero = true;
            foreach (var item in data)
            {
                if (item == 0 & leadingZero)
                {
                    continue;
                }
                else
                {
                    leadingZero = false;
                }

                yield return item;
            }
        }

        /// <summary>
        /// Creates an instance of the specified type using that type's default constructor.
        /// </summary>
        /// <typeparam name="T">The type to create.</typeparam>
        /// <param name="type">Type of the instance to create.</param>
        /// <returns>A reference to the newly created object.</returns>
        internal static T CreateInstance<T>(this Type type) where T : class
        {
            if (type == null)
                return null;
            return Activator.CreateInstance(type) as T;
        }

        /// <summary>
        /// Returns the specified 16-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        internal static byte[] GetBytes(this UInt16 value)
        {
            return new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        }

        /// <summary>
        /// Returns the specified 32-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        internal static byte[] GetBytes(this UInt32 value)
        {
            return new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value & 0xFF) };
        }

        /// <summary>
        /// Returns the specified 64-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        internal static byte[] GetBytes(this UInt64 value)
        {
            return new byte[] { (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32), (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value & 0xFF) };
        }

        /// <summary>
        /// Returns the specified 64-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        internal static byte[] GetBytes(this Int64 value)
        {
            return new byte[] { (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32), (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value & 0xFF) };
        }

#if SILVERLIGHT
        private static Regex _rehost = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$|^(([a-zA-Z]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$", RegexOptions.IgnoreCase);

        private static Regex _reIPv6 = new Regex(@"^(((?=(?>.*?::)(?!.*::)))(::)?([0-9A-F]{1,4}::?){0,5}|([0-9A-F]{1,4}:){6})(\2([0-9A-F]{1,4}(::?|$)){0,2}|((25[0-5]|(2[0-4]|1\d|[1-9])?\d)(\.|$)){4}|[0-9A-F]{1,4}:[0-9A-F]{1,4})(?<![^:]:|\.)\z", RegexOptions.IgnoreCase);
#else
        private static Regex _rehost = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$|^(([a-zA-Z]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static Regex _reIPv6 = new Regex(@"^(((?=(?>.*?::)(?!.*::)))(::)?([0-9A-F]{1,4}::?){0,5}|([0-9A-F]{1,4}:){6})(\2([0-9A-F]{1,4}(::?|$)){0,2}|((25[0-5]|(2[0-4]|1\d|[1-9])?\d)(\.|$)){4}|[0-9A-F]{1,4}:[0-9A-F]{1,4})(?<![^:]:|\.)\z", RegexOptions.IgnoreCase | RegexOptions.Compiled);

#endif

        internal static bool IsValidHost(this string value)
        {
            if (value == null)
                return false;

            if (_rehost.Match(value).Success)
                return true;

            if (_reIPv6.Match(value).Success)
                return true;

            return false;
        }

        internal static bool IsValidPort(this uint value)
        {
            if (value < IPEndPoint.MinPort)
                return false;

            if (value > IPEndPoint.MaxPort)
                return false;
            return true;
        }

        internal static bool IsValidPort(this int value)
        {
            if (value < IPEndPoint.MinPort)
                return false;

            if (value > IPEndPoint.MaxPort)
                return false;
            return true;
        }

    }
}
