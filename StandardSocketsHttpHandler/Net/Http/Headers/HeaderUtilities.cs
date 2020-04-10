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
        private const string qualityName = "q";

        internal const string ConnectionClose = "close";
        internal static readonly TransferCodingHeaderValue TransferEncodingChunked =
            new TransferCodingHeaderValue("chunked");
        internal static readonly NameValueWithParametersHeaderValue ExpectContinue =
            new NameValueWithParametersHeaderValue("100-continue");

        internal const string BytesUnit = "bytes";

        // Validator
        internal static readonly Action<HttpHeaderValueCollection<string>, string> TokenValidator = ValidateToken;

        private static readonly char[] s_hexUpperChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        internal static void SetQuality(ObjectCollection<NameValueHeaderValue> parameters, double? value)
        {
            Debug.Assert(parameters != null);

            NameValueHeaderValue qualityParameter = NameValueHeaderValue.Find(parameters, qualityName);
            if (value.HasValue)
            {
                // Note that even if we check the value here, we can't prevent a user from adding an invalid quality
                // value using Parameters.Add(). Even if we would prevent the user from adding an invalid value
                // using Parameters.Add() he could always add invalid values using HttpHeaders.AddWithoutValidation().
                // So this check is really for convenience to show users that they're trying to add an invalid 
                // value.
                if ((value < 0) || (value > 1))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                string qualityString = ((double)value).ToString("0.0##", NumberFormatInfo.InvariantInfo);
                if (qualityParameter != null)
                {
                    qualityParameter.Value = qualityString;
                }
                else
                {
                    parameters.Add(new NameValueHeaderValue(qualityName, qualityString));
                }
            }
            else
            {
                // Remove quality parameter
                if (qualityParameter != null)
                {
                    parameters.Remove(qualityParameter);
                }
            }
        }

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

        internal static double? GetQuality(ObjectCollection<NameValueHeaderValue> parameters)
        {
            Debug.Assert(parameters != null);

            NameValueHeaderValue qualityParameter = NameValueHeaderValue.Find(parameters, qualityName);
            if (qualityParameter != null)
            {
                // Note that the RFC requires decimal '.' regardless of the culture. I.e. using ',' as decimal
                // separator is considered invalid (even if the current culture would allow it).
                double qualityValue = 0;
                if (double.TryParse(qualityParameter.Value, NumberStyles.AllowDecimalPoint,
                    NumberFormatInfo.InvariantInfo, out qualityValue))
                {
                    return qualityValue;
                }
                // If the stored value is an invalid quality value, just return null and log a warning. 
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, String.Format(SR.net_http_log_headers_invalid_quality, qualityParameter.Value));
            }
            return null;
        }

        internal static void CheckValidToken(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
            }

            if (HttpRuleParser.GetTokenLength(value, 0) != value.Length)
            {
                throw new FormatException(String.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        private static void ValidateToken(HttpHeaderValueCollection<string> collection, string value)
        {
            CheckValidToken(value, "item");
        }
    }
}
