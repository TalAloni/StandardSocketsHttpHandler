// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Net.Http.Headers
{
    internal sealed class GenericHeaderParser : BaseHeaderParser
    {
        private delegate int GetParsedValueLengthDelegate(string value, int startIndex, out object parsedValue);

        #region Parser instances

        internal static readonly GenericHeaderParser HostParser = new GenericHeaderParser(false, ParseHost, StringComparer.OrdinalIgnoreCase);
        internal static readonly GenericHeaderParser TokenListParser = new GenericHeaderParser(true, ParseTokenList, StringComparer.OrdinalIgnoreCase);
        internal static readonly GenericHeaderParser SingleValueNameValueWithParametersParser = new GenericHeaderParser(false, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength);
        internal static readonly GenericHeaderParser MultipleValueNameValueWithParametersParser = new GenericHeaderParser(true, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength);
        internal static readonly GenericHeaderParser SingleValueNameValueParser = new GenericHeaderParser(false, ParseNameValue);
        internal static readonly GenericHeaderParser MultipleValueNameValueParser = new GenericHeaderParser(true, ParseNameValue);
        internal static readonly GenericHeaderParser MailAddressParser = new GenericHeaderParser(false, ParseMailAddress);
        internal static readonly GenericHeaderParser SingleValueProductParser = new GenericHeaderParser(false, ParseProduct);
        internal static readonly GenericHeaderParser MultipleValueProductParser = new GenericHeaderParser(true, ParseProduct);
        internal static readonly GenericHeaderParser RangeConditionParser = new GenericHeaderParser(false, RangeConditionHeaderValue.GetRangeConditionLength);
        internal static readonly GenericHeaderParser SingleValueAuthenticationParser = new GenericHeaderParser(false, AuthenticationHeaderValue.GetAuthenticationLength);
        internal static readonly GenericHeaderParser MultipleValueAuthenticationParser = new GenericHeaderParser(true, AuthenticationHeaderValue.GetAuthenticationLength);
        internal static readonly GenericHeaderParser RangeParser = new GenericHeaderParser(false, RangeHeaderValue.GetRangeLength);
        internal static readonly GenericHeaderParser RetryConditionParser = new GenericHeaderParser(false, RetryConditionHeaderValue.GetRetryConditionLength);
        internal static readonly GenericHeaderParser ContentRangeParser = new GenericHeaderParser(false, ContentRangeHeaderValue.GetContentRangeLength);
        internal static readonly GenericHeaderParser ContentDispositionParser = new GenericHeaderParser(false, ContentDispositionHeaderValue.GetDispositionTypeLength);
        internal static readonly GenericHeaderParser SingleValueStringWithQualityParser = new GenericHeaderParser(false, StringWithQualityHeaderValue.GetStringWithQualityLength);
        internal static readonly GenericHeaderParser MultipleValueStringWithQualityParser = new GenericHeaderParser(true, StringWithQualityHeaderValue.GetStringWithQualityLength);
        internal static readonly GenericHeaderParser SingleValueEntityTagParser = new GenericHeaderParser(false, ParseSingleEntityTag);
        internal static readonly GenericHeaderParser MultipleValueEntityTagParser = new GenericHeaderParser(true, ParseMultipleEntityTags);
        internal static readonly GenericHeaderParser SingleValueViaParser = new GenericHeaderParser(false, ViaHeaderValue.GetViaLength);
        internal static readonly GenericHeaderParser MultipleValueViaParser = new GenericHeaderParser(true, ViaHeaderValue.GetViaLength);
        internal static readonly GenericHeaderParser SingleValueWarningParser = new GenericHeaderParser(false, WarningHeaderValue.GetWarningLength);
        internal static readonly GenericHeaderParser MultipleValueWarningParser = new GenericHeaderParser(true, WarningHeaderValue.GetWarningLength);

        #endregion

        private GetParsedValueLengthDelegate _getParsedValueLength;
        private IEqualityComparer _comparer;

        public override IEqualityComparer Comparer
        {
            get { return _comparer; }
        }

        private GenericHeaderParser(bool supportsMultipleValues, GetParsedValueLengthDelegate getParsedValueLength)
            : this(supportsMultipleValues, getParsedValueLength, null)
        {
        }

        private GenericHeaderParser(bool supportsMultipleValues, GetParsedValueLengthDelegate getParsedValueLength,
            IEqualityComparer comparer)
            : base(supportsMultipleValues)
        {
            Debug.Assert(getParsedValueLength != null);

            _getParsedValueLength = getParsedValueLength;
            _comparer = comparer;
        }
    }
}
