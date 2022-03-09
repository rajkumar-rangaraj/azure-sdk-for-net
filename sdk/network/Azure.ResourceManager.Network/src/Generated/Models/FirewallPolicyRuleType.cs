// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System;
using System.ComponentModel;

namespace Azure.ResourceManager.Network.Models
{
    /// <summary> Rule Type. </summary>
    internal readonly partial struct FirewallPolicyRuleType : IEquatable<FirewallPolicyRuleType>
    {
        private readonly string _value;

        /// <summary> Initializes a new instance of <see cref="FirewallPolicyRuleType"/>. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is null. </exception>
        public FirewallPolicyRuleType(string value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        private const string ApplicationRuleValue = "ApplicationRule";
        private const string NetworkRuleValue = "NetworkRule";
        private const string NatRuleValue = "NatRule";

        /// <summary> ApplicationRule. </summary>
        public static FirewallPolicyRuleType ApplicationRule { get; } = new FirewallPolicyRuleType(ApplicationRuleValue);
        /// <summary> NetworkRule. </summary>
        public static FirewallPolicyRuleType NetworkRule { get; } = new FirewallPolicyRuleType(NetworkRuleValue);
        /// <summary> NatRule. </summary>
        public static FirewallPolicyRuleType NatRule { get; } = new FirewallPolicyRuleType(NatRuleValue);
        /// <summary> Determines if two <see cref="FirewallPolicyRuleType"/> values are the same. </summary>
        public static bool operator ==(FirewallPolicyRuleType left, FirewallPolicyRuleType right) => left.Equals(right);
        /// <summary> Determines if two <see cref="FirewallPolicyRuleType"/> values are not the same. </summary>
        public static bool operator !=(FirewallPolicyRuleType left, FirewallPolicyRuleType right) => !left.Equals(right);
        /// <summary> Converts a string to a <see cref="FirewallPolicyRuleType"/>. </summary>
        public static implicit operator FirewallPolicyRuleType(string value) => new FirewallPolicyRuleType(value);

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => obj is FirewallPolicyRuleType other && Equals(other);
        /// <inheritdoc />
        public bool Equals(FirewallPolicyRuleType other) => string.Equals(_value, other._value, StringComparison.InvariantCultureIgnoreCase);

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
        /// <inheritdoc />
        public override string ToString() => _value;
    }
}
