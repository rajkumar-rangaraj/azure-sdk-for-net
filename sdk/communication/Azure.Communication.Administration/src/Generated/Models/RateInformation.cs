// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

namespace Azure.Communication.Administration.Models
{
    /// <summary> Represents a wrapper of rate information. </summary>
    public partial class RateInformation
    {
        /// <summary> Initializes a new instance of RateInformation. </summary>
        internal RateInformation()
        {
        }

        /// <summary> Initializes a new instance of RateInformation. </summary>
        /// <param name="monthlyRate"> The monthly rate of a phone plan group. </param>
        /// <param name="currencyType"> The currency of a phone plan group. </param>
        /// <param name="rateErrorMessage"> The error code of a phone plan group. </param>
        internal RateInformation(double? monthlyRate, CurrencyType? currencyType, string rateErrorMessage)
        {
            MonthlyRate = monthlyRate;
            CurrencyType = currencyType;
            RateErrorMessage = rateErrorMessage;
        }

        /// <summary> The monthly rate of a phone plan group. </summary>
        public double? MonthlyRate { get; }
        /// <summary> The currency of a phone plan group. </summary>
        public CurrencyType? CurrencyType { get; }
        /// <summary> The error code of a phone plan group. </summary>
        public string RateErrorMessage { get; }
    }
}
