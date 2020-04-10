// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace System.Net.Http.Headers
{
    internal static class HeaderUtilities
    {
        private static readonly char[] s_hexUpperChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        internal static bool ContainsNonAscii(string input)
        {
            Debug.Assert(input != null);

            foreach (char c in input)
            {
                if ((int)c > 0x7f)
                {
                    return true;
                }
            }
            return false;
        }

        // Encode a string using RFC 5987 encoding.
        // encoding'lang'PercentEncodedSpecials
        internal static string Encode5987(string input)
        {
            // Encode a string using RFC 5987 encoding.
            // encoding'lang'PercentEncodedSpecials
            StringBuilder builder = StringBuilderCache.Acquire();
            byte[] utf8bytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(input.Length));
            int utf8length = Encoding.UTF8.GetBytes(input, 0, input.Length, utf8bytes, 0);

            builder.Append("utf-8\'\'");
            for (int i = 0; i < utf8length; i++)
            {
                byte utf8byte = utf8bytes[i];

                // attr-char = ALPHA / DIGIT / "!" / "#" / "$" / "&" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
                //      ; token except ( "*" / "'" / "%" )
                if (utf8byte > 0x7F) // Encodes as multiple utf-8 bytes
                {
                    AddHexEscaped(utf8byte, builder);
                }
                else if (!HttpRuleParser.IsTokenChar((char)utf8byte) || utf8byte == '*' || utf8byte == '\'' || utf8byte == '%')
                {
                    // ASCII - Only one encoded byte.
                    AddHexEscaped(utf8byte, builder);
                }
                else
                {
                    builder.Append((char)utf8byte);
                }

            }

            Array.Clear(utf8bytes, 0, utf8length);
            ArrayPool<byte>.Shared.Return(utf8bytes);

            return StringBuilderCache.GetStringAndRelease(builder);
        }

        /// <summary>Transforms an ASCII character into its hexadecimal representation, adding the characters to a StringBuilder.</summary>
        private static void AddHexEscaped(byte c, StringBuilder destination)
        {
            Debug.Assert(destination != null);

            destination.Append('%');
            destination.Append(s_hexUpperChars[(c & 0xf0) >> 4]);
            destination.Append(s_hexUpperChars[c & 0xf]);
        }
    }
}
